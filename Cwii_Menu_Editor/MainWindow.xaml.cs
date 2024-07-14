﻿using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Cwii_Menu_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public unsafe partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }
        private byte* rom;
        private nint mb1;
        private nint mb2;
        private int language;
        private int index;

        static bool[] BytesToBitSet(byte[] bytes)
        {
            bool[] bitSet = new bool[bytes.Length * 8];
            for (int i = 0; i < bytes.Length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    // 位掩码用于检查每个位
                    bitSet[i * 8 + j] = (bytes[i] & (1 << j)) != 0;
                }
            }
            return bitSet;
        }
        bool[] BytesToBitSet(byte* bytes, nint length)
        {
            bool[] bitSet = new bool[length * 8];
            if (bytes + length >= rom + 0x80000)
            {
                return bitSet;
            }
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    // 位掩码用于检查每个位
                    bitSet[i * 8 + (7 - j)] = (bytes[i] & (1 << j)) != 0;
                }
            }
            return bitSet;
        }
        private ImageSource ComposeImage()
        {
            WriteableBitmap b = new(64, 28, 96, 96, PixelFormats.Bgr32, null);
            b.Lock();
            byte* buf = (byte*)b.BackBuffer;
            // render icon
            {
                nint rb1 = *(int*)(rom + mb1 + index * 8) & 0xffffff;
                nint rb2 = *(int*)(rom + mb1 + index * 8 + 4) & 0xffffff;
                var array1 = BytesToBitSet(rom + rb1, 2 * 64);
                var array2 = BytesToBitSet(rom + rb2, 2 * 64);
                byte[] fin = array1.Zip(array2, (a, b) => (byte)((b ? 2 : 0) + (a ? 1 : 0))).ToArray();
                int d = 0;
                for (int j = 0; j < 16; j++)
                {
                    for (int i = 0; i < 64; i++)
                    {
                        buf[j * b.BackBufferStride + i * 4] = (byte)(255 - fin[d] * 85);
                        buf[j * b.BackBufferStride + i * 4 + 1] = (byte)(255 - fin[d] * 85);
                        buf[j * b.BackBufferStride + i * 4 + 2] = (byte)(255 - fin[d++] * 85);
                    }
                }
            }

            // render text
            {
                nint rb1 = *(int*)(rom + mb2 + index * 4 + language * 0x3c) & 0xffffff;
                var array1 = BytesToBitSet(rom + rb1, 2 * 64);
                int d = 0;
                for (int j = 15; j < 28; j++)
                {
                    for (int i = 0; i < 64; i++)
                    {
                        buf[j * b.BackBufferStride + i * 4] = (byte)(!array1[d] ? 255 : 0);
                        buf[j * b.BackBufferStride + i * 4 + 1] = (byte)(!array1[d] ? 255 : 0);
                        buf[j * b.BackBufferStride + i * 4 + 2] = (byte)(!array1[d++] ? 255 : 0);
                    }
                }

            }
            b.AddDirtyRect(new Int32Rect(0, 0, 64, 28));
            b.Unlock();
            return b;
        }
        // Load Rom
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new();
            ofd.ShowDialog();
            var stm = ofd.OpenFile();
            if (rom != null)
                Marshal.FreeHGlobal((nint)rom);
            rom = (byte*)Marshal.AllocHGlobal(0x80000);
            stm.Read(new Span<byte>(rom, 0x80000));
            stm.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            mb1 = Convert.ToInt32(MenuBase1Input.Text, 16);
            mb2 = Convert.ToInt32(MenuBase2Input.Text, 16);
            language = Convert.ToInt32(LanguageInput.Text, 16);
            index = Convert.ToInt32(IndexInput.Text, 16);
            PreviewImage.Source = ComposeImage();
            ModeInput.Text = Convert.ToString(*(rom + mb1 + index * 8 + 7), 16);
            BitmapOffset0.Text = Convert.ToString(*(uint*)(rom + mb1 + index * 8) & 0xffffffu, 16);
            BitmapOffset1.Text = Convert.ToString(*(uint*)(rom + mb1 + index * 8 + 4) & 0xffffffu, 16);
            BitmapOffset2.Text = Convert.ToString(*(uint*)(rom + mb2 + index * 4 + language * 0x3c) & 0xffffffu, 16);
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            *(rom + mb1 + index * 8 + 7) = Convert.ToByte(ModeInput.Text, 16);
        }
        // BitmapOffset1
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            *(uint*)(rom + mb1 + index * 8 + 4) = (Convert.ToUInt32(BitmapOffset1.Text, 16) & 0xffffffu) | (*(uint*)(rom + mb1 + index * 8 + 4) & 0xff000000u);
        }
        // BitmapOffset0
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            *(uint*)(rom + mb1 + index * 8) = (Convert.ToUInt32(BitmapOffset0.Text, 16) & 0xffffffu) | (*(uint*)(rom + mb1 + index * 8) & 0xff000000u);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new();
            sfd.ShowDialog();
            var fs = sfd.OpenFile();
            fs.Write(new ReadOnlySpan<byte>(rom, 0x80000));
            fs.Write(new ReadOnlySpan<byte>(rom, 0x80000));
            fs.Close();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            *(uint*)(rom + mb2 + index * 4 + language * 0x3c) = (Convert.ToUInt32(BitmapOffset2.Text, 16) & 0xffffffu) | (*(uint*)(rom + mb2 + index * 4 + language * 0x3c) & 0xff000000u);
        }
        private void SaveImage(BitmapSource bmp, Stream s)
        {
            try
            {
                // 创建 PngBitmapEncoder 并设置编码属性
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(s);

                MessageBox.Show("成功保存");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"错误: \n{ex}");
            }
        }
        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new();
            ofd.ShowDialog();
            var stm = ofd.OpenFile();
            BitmapImage bi = new();
            bi.BeginInit();
            bi.StreamSource = stm;
            bi.EndInit();
            if (bi.PixelWidth < 64 || bi.PixelHeight < 15)
            {
                MessageBox.Show("请选择64*15的图片.");
                return;
            }
            SetIcon(bi, new Int32Rect(0, 0, 64, 15));
        }

        private void SetIcon(BitmapImage bi, Int32Rect range)
        {
            byte* data = stackalloc byte[64 * 15 * 4];
            bi.CopyPixels(range, (nint)data, 64 * 15 * 4, 64 * 4);
            nint rb1 = *(int*)(rom + mb1 + index * 8) & 0xffffff;
            nint rb2 = *(int*)(rom + mb1 + index * 8 + 4) & 0xffffff;

            byte* array1 = rom + rb1;
            byte* array2 = rom + rb2;
            for (int i = 0; i < 8 * 16; i++)
            {
                array1[i] = 0;
                array2[i] = 0;
            }
            //byte[] fin = array1.Zip(array2, (a, b) => (byte)((b ? 2 : 0) + (a ? 1 : 0))).ToArray();
            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        var b =
                            data[j * 64 * 4 + i * 8 * 4 + k * 4 + 2] >> 6;
                        array2[j * 8 + i] |= (b & 0b10) == 0 ? (byte)(0x80 >> k) : (byte)0;
                        array1[j * 8 + i] |= (b & 0b1) == 0 ? (byte)(0x80 >> k) : (byte)0;
                    }
                }
            }
        }
        private void SetLabel(BitmapImage bi, Int32Rect range)
        {
            byte* data = stackalloc byte[64 * 15 * 4];
            bi.CopyPixels(range, (nint)data, 64 * 15 * 4, 64 * 4);
            nint rb1 = *(int*)(rom + mb2 + index * 4 + language * 0x3c) & 0xffffff;

            byte* array1 = rom + rb1;
            for (int i = 0; i < 13*8; i++)
            {
                array1[i] = 0;
            }
            //byte[] fin = array1.Zip(array2, (a, b) => (byte)((b ? 2 : 0) + (a ? 1 : 0))).ToArray();
            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        var b = data[j * 64 * 4 + i * 8 * 4 + k * 4 + 2] >> 7;
                        array1[j * 8 + i] |= (b & 0b1) == 0 ? (byte)(0x80 >> k) : (byte)0;
                    }
                }
            }
        }
        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            if (PreviewImage.Source == null)
            {
                MessageBox.Show("请先加载.");
                return;
            }
            SaveFileDialog sfd = new();
            sfd.ShowDialog();
            var fs = sfd.OpenFile();
            SaveImage((BitmapSource?)PreviewImage.Source, fs);
            fs.Close();
        }

        private void Button_Click_9(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new();
            ofd.ShowDialog();
            var stm = ofd.OpenFile();
            BitmapImage bi = new();
            bi.BeginInit();
            bi.StreamSource = stm;
            bi.EndInit();
            if (bi.PixelWidth < 64 || bi.PixelHeight < 13)
            {
                MessageBox.Show("请选择64*13的图片.");
                return;
            }
            SetLabel(bi, new Int32Rect(0, 0, 64, 13));
        }

        private void Button_Click_10(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new();
            ofd.ShowDialog();
            var stm = ofd.OpenFile();
            BitmapImage bi = new();
            bi.BeginInit();
            bi.StreamSource = stm;
            bi.EndInit();
            if (bi.PixelWidth < 64 || bi.PixelHeight < 28)
            {
                MessageBox.Show("请选择64*15的图片.");
                return;
            }
            SetIcon(bi, new Int32Rect(0, 0, 64, 15));
            SetLabel(bi, new Int32Rect(0, 15, 64, 13));
        }
    }
}