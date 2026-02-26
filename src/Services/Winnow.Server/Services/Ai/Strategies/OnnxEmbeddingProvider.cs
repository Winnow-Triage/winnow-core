using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;
using Winnow.Server.Infrastructure.Configuration;

namespace Winnow.Server.Services.Ai.Strategies;

internal class OnnxEmbeddingProvider : IEmbeddingProvider, IDisposable
{
    private readonly InferenceSession? _session;
    private readonly Tokenizer? _tokenizer;
    private readonly ILogger<OnnxEmbeddingProvider> _logger;
    private readonly string _modelPath;
    private readonly string _modelDir;
    private readonly Random _rng = new();

    public OnnxEmbeddingProvider(ILogger<OnnxEmbeddingProvider> logger, IHostEnvironment env)
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
                else
                {
                    _logger.LogInformation("OnnxEmbeddingProvider: Successfully loaded ONNX model and Tokenizer from {Dir}", _modelDir);
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
            _logger.LogWarning("OnnxEmbeddingProvider: Using MOCK embedding (Model={Model}, Tokenizer={Tok})", _session != null, _tokenizer != null);
            return Task.FromResult(GenerateMockEmbedding());
        }

        // Offload the heavy CPU work to a background thread to keep the Request Thread free
        return Task.Run(() =>
        {
            try
            {
                text = text.ToLowerInvariant();

                // 1. Tokenize
                var rawIds = _tokenizer.EncodeToIds(text).Select(x => (long)x).ToList();
                _logger.LogDebug("OnnxEmbeddingProvider: Tokenized {Len} chars into {Count} tokens.", text.Length, rawIds.Count);

                // 2. Truncate & Construct (Safe Method)
                int maxContentTokens = 512 - 2;
                List<long> tokenIdsList = [101]; // [CLS]
                tokenIdsList.AddRange(rawIds.Take(maxContentTokens));
                tokenIdsList.Add(102); // [SEP]

                var tokenIds = tokenIdsList.ToArray();
                var dimensions = new[] { 1, tokenIds.Length };
                var inputIdsTensor = new DenseTensor<long>(tokenIds, dimensions);

                var attentionMaskData = tokenIds.Select(id => id == 0 ? 0L : 1L).ToArray();
                var attentionMaskTensor = new DenseTensor<long>(attentionMaskData, dimensions);
                var tokenTypeIdsTensor = new DenseTensor<long>(new long[tokenIds.Length], dimensions);

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
                    NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
                    NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
                };

                using var results = _session.Run(inputs);
                var outputTensor = results[0].AsTensor<float>();

                // 5. Mean Pooling
                int hiddenSize = 384;
                var pooled = new float[hiddenSize];
                int validTokenCount = 0;

                for (int i = 0; i < tokenIds.Length; i++)
                {
                    if (tokenIds[i] == 0) continue;
                    validTokenCount++;
                    for (int j = 0; j < hiddenSize; j++)
                    {
                        pooled[j] += outputTensor[0, i, j];
                    }
                }

                if (validTokenCount > 0)
                {
                    for (int i = 0; i < hiddenSize; i++) pooled[i] /= validTokenCount;
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
                return GenerateMockEmbedding();
            }
        });
    }

    public bool CanHandle(LlmSettings settings)
    {
        // ONNX provider is always available as a fallback, but we prefer it when no specific provider is configured
        // or when the provider is set to "Placeholder" or "Onnx"
        return string.IsNullOrEmpty(settings?.EmbeddingProvider) ||
               settings.EmbeddingProvider.Equals("Placeholder", StringComparison.OrdinalIgnoreCase) ||
               settings.EmbeddingProvider.Equals("Onnx", StringComparison.OrdinalIgnoreCase) ||
               (File.Exists(_modelPath) && _session != null && _tokenizer != null);
    }

    private float[] GenerateMockEmbedding()
    {
        var embedding = new float[384];
        for (int i = 0; i < embedding.Length; i++)
        {
            embedding[i] = (float)(_rng.NextDouble() * 2 - 1);
        }
        return embedding;
    }

    public void Dispose()
    {
        _session?.Dispose();
        GC.SuppressFinalize(this);
    }
}