using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace Winnow.Server.Services.Ai;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
}

public class EmbeddingService : IEmbeddingService, IDisposable
{
    private readonly InferenceSession? _session;
    private readonly Tokenizer? _tokenizer;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly string _modelPath;
    private readonly string _modelDir;

    public EmbeddingService(ILogger<EmbeddingService> logger, IHostEnvironment env)
    {
        _logger = logger;
        _modelDir = Path.Combine(env.ContentRootPath, "AiModel");
        _modelPath = Path.Combine(_modelDir, "model.onnx");

        try
        {
            if (File.Exists(_modelPath))
            {
                var sessionOptions = new Microsoft.ML.OnnxRuntime.SessionOptions();
                _session = new InferenceSession(_modelPath, sessionOptions);

                var vocabPath = Path.Combine(_modelDir, "vocab.txt");

                if (File.Exists(vocabPath))
                {
                    try
                    {
                        using var stream = File.OpenRead(vocabPath);
                        _tokenizer = WordPieceTokenizer.Create(stream);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create WordPieceTokenizer from vocab.txt.");
                    }
                }

                if (_tokenizer == null)
                {
                    _logger.LogWarning("Tokenizer vocab not found or failed to load in {Dir}. Using Mock.", _modelDir);
                }
            }
            else
            {
                _logger.LogWarning("ONNX model not found at {Path}. Using Mock.", _modelPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ONNX model implementation.");
            _session = null;
            _tokenizer = null;
        }
    }

    public Task<float[]> GetEmbeddingAsync(string text)
    {
        if (_session == null || _tokenizer == null)
        {
            return Task.FromResult(MockEmbedding());
        }

        // Offload the heavy CPU work to a background thread to keep the Request Thread free
        return Task.Run(() =>
        {
            try
            {
                text = text.ToLowerInvariant();

                // 1. Tokenize
                // Note: EncodeToIds does NOT add special tokens by default in this library
                var rawIds = _tokenizer.EncodeToIds(text).Select(x => (long)x).ToList();

                // 2. Truncate & Construct (Safe Method)
                // Max capacity = 512. We need 2 slots for [CLS] and [SEP].
                // So we can take at most 510 real tokens.
                int maxContentTokens = 512 - 2;

                var tokenIdsList = new List<long>(512);
                tokenIdsList.Add(101); // [CLS]
                tokenIdsList.AddRange(rawIds.Take(maxContentTokens)); // Truncate content, not the whole list
                tokenIdsList.Add(102); // [SEP] Always add this at the end

                // 3. Pad to 512 (Optional, but usually good for dense tensors)
                // Or just keep dynamic length. MiniLM handles dynamic length fine.
                // Let's stick to your dynamic approach (dimensions = [1, length]) which is faster.
                var tokenIds = tokenIdsList.ToArray();

                var dimensions = new[] { 1, tokenIds.Length };
                var inputIdsTensor = new DenseTensor<long>(tokenIds, dimensions);

                // 4. Attention Mask (CORRECTED)
                // 0 = Mask (Ignore), 1 = Pay Attention
                // ONLY mask ID 0 ([PAD]). DO NOT mask 100 ([UNK]).
                var attentionMaskData = tokenIds.Select(id => id == 0 ? 0L : 1L).ToArray();
                var attentionMaskTensor = new DenseTensor<long>(attentionMaskData, dimensions);

                // Token Types are always 0 for single sentences
                var tokenTypeIdsTensor = new DenseTensor<long>(new long[tokenIds.Length], dimensions);

                var inputs = new List<NamedOnnxValue>
                {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
                };

                using var results = _session.Run(inputs);
                var outputTensor = results.First().AsTensor<float>();

                // 5. Mean Pooling (CORRECTED)
                int hiddenSize = 384;
                var pooled = new float[hiddenSize];
                int validTokenCount = 0;

                for (int i = 0; i < tokenIds.Length; i++)
                {
                    // Strict Mean Pooling:
                    // Only skip Padding (0). 
                    // Standard SBERT usually INCLUDES [CLS] and [SEP] in the average.
                    // If you want to be strict about "content only", skipping 101/102 is fine,
                    // BUT definitely do NOT skip 100 ([UNK]).
                    if (tokenIds[i] == 0) continue;

                    // Optional: If you strictly want content words only, uncomment this:
                    // if (tokenIds[i] == 101 || tokenIds[i] == 102) continue;

                    validTokenCount++;
                    for (int j = 0; j < hiddenSize; j++)
                    {
                        pooled[j] += outputTensor[0, i, j];
                    }
                }

                // Average
                if (validTokenCount > 0)
                {
                    for (int i = 0; i < hiddenSize; i++)
                    {
                        pooled[i] /= validTokenCount;
                    }
                }

                // 6. Normalize
                float norm = 0;
                for (int i = 0; i < hiddenSize; i++) norm += pooled[i] * pooled[i];
                norm = (float)Math.Sqrt(norm);

                if (norm > 1e-6)
                {
                    for (int i = 0; i < hiddenSize; i++) pooled[i] /= norm;
                }

                return pooled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Embedding generation failed.");
                return MockEmbedding();
            }
        });
    }

    private float[] MockEmbedding()
    {
        var rng = new Random();
        var embedding = new float[384];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(rng.NextDouble() * 2 - 1);
        }
        return embedding;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
