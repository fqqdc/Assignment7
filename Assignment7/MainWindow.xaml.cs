﻿using Assignment6;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using Vector3f = System.Numerics.Vector3;
using System.Threading;
using System.Linq;
using AForge.Imaging.Filters;
using System.Drawing;
using System.IO;

namespace Assignment7
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeScene();

            Loaded += MainWindow_Loaded;
        }

        void RandomSort(int[] array)
        {
            Random random = new Random();
            int last = array.Length - 1;
            for (int i = 0; i < array.Length; i++)
            {
                int randomIndex = random.Next(array.Length);
                int temp = array[last];
                array[last] = array[randomIndex];
                array[randomIndex] = temp;
                last--;//位置改变
            }
        }

        // 去噪
        public static ImageSource MedianFilter(ImageSource inputImage, int filterSize)
        {
            var bitmap = ImageSourceToBitmap(inputImage);
            if (bitmap == null) throw new NullReferenceException();

            Median filter = new Median(filterSize);
            Bitmap bitmapApply = filter.Apply(bitmap);


            return BitmapToImageSource(bitmapApply);
        }

        // 将System.Drawing.Bitmap对象转换为System.Windows.Controls.ImageSource对象
        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
            bitmapImage.EndInit();

            return bitmapImage;
        }

        // 将System.Windows.Controls.ImageSource对象转换为System.Drawing.Bitmap对象
        public static Bitmap? ImageSourceToBitmap(ImageSource imageSource)
        {
            Bitmap? bitmap = null;

            if (imageSource is BitmapSource bitmapSource)
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    BitmapEncoder encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                    encoder.Save(memoryStream);

                    bitmap = new Bitmap(memoryStream);
                }
            }

            return bitmap;
        }


        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;

            using var threadRandom = new ThreadLocal<Random>();
            Global.RandomGenerator = () => threadRandom.Value ?? (threadRandom.Value = new());

            Stopwatch sw = Stopwatch.StartNew();
            progressBarText.Text = $"0%";
            progressBarViewer.Visibility = Visibility.Visible;

            byte[] data = new byte[0];
            var renderer = new Renderer();
            WriteableBitmap wb = new(scene.Width, scene.Height,
                96, 96, PixelFormats.Bgr24, null);
            var updateTask = Task.Run(UpdateProgressTask);
            //data = await Task.Run(() => renderer.Render(scene, 2)); //单线程 像素采样率x，每个像素采样x^2次
            //data = await Task.Run(() => renderer.RenderParallel(scene, 8)); //多线程 像素采样率x，每个像素采样x^2次
            //data = await Task.Run(() => renderer.RenderGPU(scene, 8, preferCPU: false)); //ILGPU库 像素采样率x，每个像素采样x^2次

            {
                int ssLevel = 16;
                Vector3f[] framebuffer = new Vector3f[scene.Width * scene.Height];
                for (int i = 0; i < ssLevel * ssLevel; i++)
                {
                    data = await Task.Run(() => renderer.RenderSingleStepByFrame(scene, ssLevel, framebuffer, i));
                    wb.WritePixels(new(0, 0, scene.Width, scene.Height),
                        data, scene.Width * 3, 0);
                    image.Source = wb;
                    Global.UpdateProgress((i + 1) / (float)(ssLevel * ssLevel));
                }
                Global.UpdateProgress(1);
                await updateTask;
            }

            //{
            //    int ssLevel = 8;
            //    data = new byte[scene.Width * scene.Height * 3];
            //    var order = Enumerable.Range(0, scene.Width * scene.Height).ToArray();
            //    RandomSort(order);
            //    for (int i = 0; i < order.Length; i++)
            //    {
            //        data = await Task.Run(() => renderer.RenderSingleStepByPixel(scene, data, ssLevel, order, ref i));
            //        wb.WritePixels(new(0, 0, scene.Width, scene.Height),
            //            data, scene.Width * 3, 0);
            //        image.Source = wb;
            //        Global.UpdateProgress((i + 1) / (float)order.Length);
            //    }
            //    Global.UpdateProgress(1);
            //    await updateTask;
            //}

            wb.WritePixels(new(0, 0, scene.Width, scene.Height),
                        data, scene.Width * 3, 0);
            image.Source = wb;
            SaveImage(wb);
            progressBarViewer.Visibility = Visibility.Collapsed;
            MessageBox.Show($"Time taken:{sw.Elapsed}", "Render Complete");
        }

        // Change the definition here to change resolution
        Scene scene = new(512, 512) // 分辨率
        {
            //RussianRoulette = .8f, // 光线反射概率
            ExpectedTime = 5, // 光线反射次数期望
            //BackgroundColor = new(0),
        };
        void InitializeScene()
        {
            Global.UpdateProgress = DefaultUpdateProgress;

            Material red = new Material(MaterialType.DIFFUSE, new(0.0f)) { Kd = new(0.63f, 0.065f, 0.05f) };
            Material green = new Material(MaterialType.DIFFUSE, new(0.0f)) { Kd = new(0.14f, 0.45f, 0.091f) };
            Material white = new Material(MaterialType.DIFFUSE, new(0.0f)) { Kd = new(0.725f, 0.71f, 0.68f) };
            Material light = new Material(MaterialType.DIFFUSE,
                8.0f * new Vector3f(0.747f + 0.058f, 0.747f + 0.258f, 0.747f)
                + 15.6f * new Vector3f(0.740f + 0.287f, 0.740f + 0.160f, 0.740f)
                + 18.4f * new Vector3f(0.737f + 0.642f, 0.737f + 0.159f, 0.737f))
            { Kd = new(0.65f) };
            Material whiteM = new Material(MaterialType.Microfacet, new(0.0f)) { Kd = new(0.725f, 0.71f, 0.68f), Ks = new(0.45f) };

            MeshTriangle floor = new("models/cornellbox/floor.obj", white);
            MeshTriangle shortbox = new("models/cornellbox/shortbox.obj", white);
            MeshTriangle tallbox = new("models/cornellbox/tallbox.obj", white);
            MeshTriangle left = new("models/cornellbox/left.obj", red);
            MeshTriangle right = new("models/cornellbox/right.obj", green);
            MeshTriangle light_ = new("models/cornellbox/light.obj", light);

            MeshTriangle bunny = new("models/bunny/bunny.obj", white, new(300, 0, 300), new(2000));
            //Sphere sphere1 = new(new(170, 110, 350), 110, whiteM);
            //Sphere sphere2 = new(new(380, 90, 450), 90, white);

            scene.Add(floor);
            scene.Add(shortbox);
            scene.Add(tallbox);
            //scene.Add(bunny);
            //scene.Add(sphere1);
            //scene.Add(sphere2);
            scene.Add(left);
            scene.Add(right);
            scene.Add(light_);

            scene.BuildBVH();
        }

        #region UI等方法
        void SaveImage(BitmapSource bs)
        {
            using var fs = new System.IO.FileStream("output.png", System.IO.FileMode.Create);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bs));
            encoder.Save(fs);
        }

        float progressValue = 0f;
        AutoResetEvent updateProgressEnent = new(false);
        void DefaultUpdateProgress(float progress)
        {
            if (progressValue < progress * 100)
            {
                progressValue = progress * 100;
                updateProgressEnent.Set();
            }
        }

        void UpdateProgressTask()
        {
            while (true)
            {
                updateProgressEnent.WaitOne();

                Dispatcher.Invoke(() =>
                {
                    progressBar.Value = Math.Max(progressBar.Value, progressValue);
                    progressBarText.Text = $"{progressBar.Value:F2}%";
                });

                if (Math.Abs(100 - progressValue) < 0.01)
                    break;
            }
        }

        void ResetProgress()
        {
            progressValue = 0;
            progressBar.Value = progressValue;
            progressBarText.Text = $"{progressBar.Value:F2}%";
        }

        #endregion
    }
}
