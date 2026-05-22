using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BLL.Services;
using DAL.Data;
using DAL.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;
using System.IO;
using System.Reflection;

namespace Benchmarks
{
    public class LicensePlateBenchmark
    {
        private LicensePlateService _service;
        private byte[] _testImage;
        private MethodInfo _cropRegionMethod;
        private object[] _methodArgs;

        [GlobalSetup]
        public void Setup()
        {
            var logger = new NullLogger<LicensePlateService>();

            // We can pass nulls for Engine and Ocr because we are only testing CropRegion
            _service = new LicensePlateService(null!, null!, logger);

            // Create a dummy image
            using var bitmap = new SKBitmap(1920, 1080);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.White);

            using var ms = new MemoryStream();
            bitmap.Encode(ms, SKEncodedImageFormat.Jpeg, 100);
            _testImage = ms.ToArray();

            _cropRegionMethod = typeof(LicensePlateService).GetMethod("CropRegion", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var box = new DAL.Entities.DetectionBox
            {
                X1 = 0.4f,
                Y1 = 0.4f,
                X2 = 0.6f,
                Y2 = 0.6f,
                Confidence = 0.9f
            };

            _methodArgs = new object[] { _testImage, box, PlatePosition.Back };
        }

        [Benchmark]
        public byte[] CropRegionBenchmark()
        {
            return (byte[])_cropRegionMethod.Invoke(_service, _methodArgs)!;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<LicensePlateBenchmark>();
        }
    }
}
