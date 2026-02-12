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
                        _logger.LogError(ex, "Failed to create WordPieceTokenizer.");
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
            // 1. Tokenize
            var tokenList = _tokenizer.EncodeToIds(text);
            var tokenIds = tokenList.Select(x => (long)x).ToArray();

            if (tokenIds.Length > 512)
            {
                tokenIds = tokenIds.Take(512).ToArray();
            }

            // 2. Prepare Inputs using DenseTensor
            // Shape: [1, seq_len]
            var dimensions = new[] { 1, tokenIds.Length };

            var inputIdsTensor = new DenseTensor<long>(tokenIds, dimensions);
            var attentionMaskTensor = new DenseTensor<long>(Enumerable.Repeat(1L, tokenIds.Length).ToArray(), dimensions);
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
            // Assuming the first output is the hidden state or embedding
            // For all-MiniLM-L6-v2, output[0] is usually 'last_hidden_state' [1, seq_len, 384]
            // We need to confirm if model output is float. Usually yes.

            var outputTensor = results.First().AsTensor<float>();

            // Mean Pooling
            int hiddenSize = 384;
            int seqLen = tokenIds.Length;

            var pooled = new float[hiddenSize];
            for (int i = 0; i < seqLen; i++)
            {
                for (int j = 0; j < hiddenSize; j++)
                {
                    // Access tensor: [0, i, j]
                    pooled[j] += outputTensor[0, i, j];
                }
            }

            // Normalize
            for (int i = 0; i < hiddenSize; i++)
            {
                pooled[i] /= seqLen;
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
