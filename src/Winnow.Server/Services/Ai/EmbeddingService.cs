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

        try
        {
            // Lowercase is critical for uncased models (like MiniLM)
            text = text.ToLowerInvariant();

            // 1. Tokenize and wrap with BERT special tokens
            var rawIds = _tokenizer.EncodeToIds(text).Select(x => (long)x).ToList();
            var tokenIdsList = new List<long> { 101 }; // [CLS]
            tokenIdsList.AddRange(rawIds);
            if (tokenIdsList.Count < 512) tokenIdsList.Add(102); // [SEP]

            var tokenIds = tokenIdsList.Take(512).ToArray();

            // 2. Prepare Inputs using DenseTensor
            // Shape: [1, seq_len]
            var dimensions = new[] { 1, tokenIds.Length };

            var inputIdsTensor = new DenseTensor<long>(tokenIds, dimensions);

            // Skip ID 0 (usually [PAD] in tokenizer, though vocab.txt said 100 for this specific one, 0 is safer fallback)
            // also skip 100 specifically if we saw it being used for padding in logs.
            var attentionMaskData = tokenIds.Select(id => (id == 0 || id == 100) ? 0L : 1L).ToArray();
            var attentionMaskTensor = new DenseTensor<long>(attentionMaskData, dimensions);
            var tokenTypeIdsTensor = new DenseTensor<long>(Enumerable.Repeat(0L, tokenIds.Length).ToArray(), dimensions);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
            };

            // 3. Run Inference
            using var results = _session.Run(inputs);

            // 4. Extract Output
            // For sentence-transformers/all-MiniLM-L6-v2, output[0] is 'last_hidden_state' [1, seq_len, 384]
            var outputTensor = results.First().AsTensor<float>();

            // MEAN POOLING: Average all non-special tokens (standard for sentence-transformers)
            int hiddenSize = 384;
            int seqLen = tokenIds.Length;
            var pooled = new float[hiddenSize];
            int validTokenCount = 0;

            for (int i = 0; i < seqLen; i++)
            {
                // Skip [PAD] (0/100), [CLS] (101), [SEP] (102)
                if (tokenIds[i] == 0 || tokenIds[i] == 100 || tokenIds[i] == 101 || tokenIds[i] == 102) continue;

                validTokenCount++;
                for (int j = 0; j < hiddenSize; j++)
                {
                    pooled[j] += outputTensor[0, i, j];
                }
            }

            if (validTokenCount > 0)
            {
                for (int i = 0; i < hiddenSize; i++)
                {
                    pooled[i] /= validTokenCount;
                }
            }

            _logger.LogInformation("Embedding generated for text (len={TextLen}). ValidTokens: {Actual}, First5Tokens: {Tokens}, First5Embed: {Embed}",
                text.Length, validTokenCount, string.Join(",", tokenIds.Take(5)), string.Join(",", pooled.Take(5)));

            // 4. L2 Normalize
            float norm = 0;
            for (int i = 0; i < hiddenSize; i++) norm += pooled[i] * pooled[i];
            norm = (float)Math.Sqrt(norm);

            if (norm > 1e-6) // Avoid divide by zero
            {
                for (int i = 0; i < hiddenSize; i++) pooled[i] /= norm;
            }

            return Task.FromResult(pooled);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding. Falling back to mock.");
            return Task.FromResult(MockEmbedding());
        }
    }

    private float[] MockEmbedding()
    {
        var rng = new Random();
        var embedding = new float[384];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)rng.NextDouble();
        }
        return embedding;
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
