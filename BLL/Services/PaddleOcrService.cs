using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SkiaSharp;

namespace BLL.Services
{
    public class PaddleOcrService : IDisposable
    {
        private readonly InferenceSession _recSession;
        private readonly ILogger<PaddleOcrService> _logger;
        private readonly string _charSet;

        public PaddleOcrService(string recModelPath, string dictPath, ILogger<PaddleOcrService> logger)
        {
            _logger = logger;
            var options = new SessionOptions();
            options.GraphOptimizationLevel =
                GraphOptimizationLevel.ORT_ENABLE_ALL;

            _recSession = new InferenceSession(recModelPath, options);

            var chars = File.ReadAllLines(dictPath)
                            .Select(l => l.TrimEnd('\r', '\n'))
                            .ToList();

            _charSet = string.Join("", chars);
            _logger.LogInformation(
                "Charset loaded: {Size} chars. First='{F}' Last='{L}'",
                _charSet.Length,
                _charSet.FirstOrDefault(),
                _charSet.LastOrDefault());
        }

        // ── Public entry point ───────────────────────────────────────────────
        public async Task<string> ExtractTextAsync(byte[] imageBytes, string plateType = "SHORT", string position = "Unknown")
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation(
                        "ExtractTextAsync — plateType: {Type} position: {Pos}",
                        plateType, position);

                    if (plateType == "LONG")
                        return ReadLongPlate(imageBytes, position);
                    else
                        return ReadTwoLinePlate(imageBytes, position);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PaddleOCR failed: {Msg}", ex.Message);
                    return string.Empty;
                }
            });
        }


        // ── Two-line plate (biển ngắn) ───────────────────────────────────────
        private string ReadTwoLinePlate(byte[] imageBytes, string position)
        {
            var upscaledBytes = UpscalePlate(imageBytes);
            using var upscaled = SKBitmap.Decode(upscaledBytes);

            int W = upscaled.Width;
            int H = upscaled.Height;

            // Find the actual gap between lines by looking for the whitest row
            // Only search between 30% and 60% of height
            int searchStart = (int)(H * 0.30f);
            int searchEnd = (int)(H * 0.60f);

            float maxWhiteness = 0f;
            int splitY = H / 2;

            for (int y = searchStart; y < searchEnd; y++)
            {
                float rowWhiteness = 0f;
                for (int x = 0; x < W; x++)
                {
                    var p = upscaled.GetPixel(x, y);
                    rowWhiteness += (p.Red + p.Green + p.Blue) / (3f * 255f);
                }
                rowWhiteness /= W;

                if (rowWhiteness > maxWhiteness)
                {
                    maxWhiteness = rowWhiteness;
                    splitY = y;
                }
            }

            _logger.LogInformation(
                "[{Pos}] Auto split at Y={S} (whiteness={W:F3}) of H={H}",
                position, splitY, maxWhiteness, H);

            using var topBmp = new SKBitmap(W, splitY);
            using var botBmp = new SKBitmap(W, H - splitY);

            upscaled.ExtractSubset(topBmp, new SKRectI(0, 0, W, splitY));
            upscaled.ExtractSubset(botBmp, new SKRectI(0, splitY, W, H));

            byte[] ToBytes(SKBitmap bmp)
            {
                using var ms = new MemoryStream();
                bmp.Encode(ms, SKEncodedImageFormat.Png, 100);
                return ms.ToArray();
            }

            var topBytes = ToBytes(topBmp);
            var botBytes = ToBytes(botBmp);

            File.WriteAllBytes(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"debug_{position}_line1.png"), topBytes);
            File.WriteAllBytes(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"debug_{position}_line2.png"), botBytes);

            var line1 = RecognizeText(topBytes);
            var line2 = RecognizeText(botBytes);

            _logger.LogInformation("[{Pos}] Line 1: '{A}'", position, line1);
            _logger.LogInformation("[{Pos}] Line 2: '{B}'", position, line2);

            return (line1 + line2).ToUpper();
        }

        // ── Upscale ──────────────────────────────────────────────────────────
        private byte[] UpscalePlate(byte[] imageBytes)
        {
            using var bitmap = SKBitmap.Decode(imageBytes);

            int scale = Math.Max(4, 400 / Math.Max(bitmap.Width, 1));
            int width = bitmap.Width * scale;
            int height = bitmap.Height * scale;

            _logger.LogInformation(
                "Upscaling {W}x{H} → {NW}x{NH}",
                bitmap.Width, bitmap.Height, width, height);

            using var resized = bitmap.Resize(
                new SKImageInfo(width, height), SKFilterQuality.High);

            int pad = 20;
            int rawW = width + pad * 2;
            int rawH = height + pad * 2;
            int finalW = ((rawW + 31) / 32) * 32;
            int finalH = ((rawH + 31) / 32) * 32;

            _logger.LogInformation(
                "Final padded size: {W}x{H}", finalW, finalH);

            using var padded = new SKBitmap(finalW, finalH);
            using (var canvas = new SKCanvas(padded))
            {
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(resized, pad, pad);
            }

            using var ms = new MemoryStream();
            padded.Encode(ms, SKEncodedImageFormat.Png, 100);
            var bytes = ms.ToArray();

            File.WriteAllBytes(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "debug_upscaled.png"), bytes);

            return bytes;
        }

        // ── Recognition ──────────────────────────────────────────────────────
        private string RecognizeText(byte[] imageBytes)
        {
            using var bitmap = SKBitmap.Decode(imageBytes);

            int recH = 32;
            float ratio = (float)bitmap.Width / Math.Max(bitmap.Height, 1);
            int recW = Math.Max(10, (int)(recH * ratio));

            using var resized = bitmap.Resize(
                new SKImageInfo(recW, recH), SKFilterQuality.High);

            var tensor = new DenseTensor<float>(new[] { 1, 3, recH, recW });

            for (int y = 0; y < recH; y++)
                for (int x = 0; x < recW; x++)
                {
                    var p = resized.GetPixel(x, y);
                    tensor[0, 0, y, x] = (p.Red / 255f - 0.5f) / 0.5f;
                    tensor[0, 1, y, x] = (p.Green / 255f - 0.5f) / 0.5f;
                    tensor[0, 2, y, x] = (p.Blue / 255f - 0.5f) / 0.5f;
                }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("x", tensor)
            };

            using var results = _recSession.Run(inputs);
            var output = results
                .First(r => r.Name == "softmax_2.tmp_0")
                .AsTensor<float>();

            return DecodeCtc(output);
        }

        // ── CTC Decoder ──────────────────────────────────────────────────────
        private string DecodeCtc(Tensor<float> output)
        {
            int T = output.Dimensions[1];
            int numClasses = output.Dimensions[2];

            var sb = new System.Text.StringBuilder();
            int lastIdx = -1;

            for (int t = 0; t < T; t++)
            {
                int maxIdx = 0;
                float maxVal = float.MinValue;

                for (int c = 0; c < numClasses; c++)
                {
                    if (output[0, t, c] > maxVal)
                    {
                        maxVal = output[0, t, c];
                        maxIdx = c;
                    }
                }

                if (maxIdx != 0 && maxIdx != lastIdx)
                {
                    int charIdx = maxIdx - 1;
                    if (charIdx < _charSet.Length)
                    {
                        sb.Append(_charSet[charIdx]);
                        _logger.LogInformation(
                            "t={T} idx={Idx} → '{Char}'",
                            t, maxIdx, _charSet[charIdx]);
                    }
                }
                lastIdx = maxIdx;
            }

            return sb.ToString();
        }

        private string ReadLongPlate(byte[] imageBytes, string position)
        {
            var upscaledBytes = UpscalePlate(imageBytes);

            File.WriteAllBytes(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"debug_{position}_long_full.png"), upscaledBytes);

            var text = RecognizeText(upscaledBytes);

            _logger.LogInformation(
                "[{Pos}] LONG plate result: '{T}'", position, text);

            return text.ToUpper();
        }

        public void Dispose() => _recSession?.Dispose();
    }

}