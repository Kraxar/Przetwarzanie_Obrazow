using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;


using NAudio.Wave; 
using NAudio.Dsp; 
using System.Drawing.Imaging; 
using System.Runtime.InteropServices; 
using System.Media;
using System.Threading;

namespace Spektogram
{
    public partial class Form1 : Form
    {     
        NAudio.Wave.WaveIn sourceStream = null;
        NAudio.Wave.DirectSoundOut waveOut = null;
        NAudio.Wave.WaveFileWriter waveWriter = null;

        SaveFileDialog save = new SaveFileDialog();

        // DOLNY SPEKTOGRAM
        private static SoundPlayer simpleSound = new SoundPlayer();
        private static int buffers_captured = 0; //  ilosc zapelnionych buforow
        private static int buffers_remaining = 0; // ilosc buforow ktore zostaly do analizowania

        private static double unanalyzed_max_sec; // maksymalna ilosc nie analizowanych danych audio trzymanych w pamieci
        private static List<short> unanalyzed_values = new List<short>(); // dane audio do analizowania

        private static List<List<double>> spec_data; // kolumny to czas , rzedy to frekwencja 
        private static int spec_width = 1000;
        private static int spec_height;
        int pixelsPerBuffer;

        private static Random rand = new Random();

        //  ustawienia karty dzwiekowej s
        private int rate;
        private int buffer_update_hz;

        // opcje spektogramu i FFT 
        int fft_size;

        public Form1()
        {
            InitializeComponent();

            Initialize_Spectrogram();
        }


        private void button4_Click(object sender, EventArgs e)
        {

            
            save.Filter = "Wave File (*.wav)|*.wav;";
            if (save.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            int deviceNumber = 0;

            sourceStream = new NAudio.Wave.WaveIn();
            sourceStream.DeviceNumber = deviceNumber;
            sourceStream.WaveFormat = new NAudio.Wave.WaveFormat(44100, NAudio.Wave.WaveIn.GetCapabilities(deviceNumber).Channels);

            sourceStream.DataAvailable += new EventHandler<NAudio.Wave.WaveInEventArgs>(sourceStream_DataAvailable);
            waveWriter = new NAudio.Wave.WaveFileWriter(save.FileName, sourceStream.WaveFormat);

            sourceStream.StartRecording();
        }

        private void sourceStream_DataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            if (waveWriter == null) return;

            waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            waveWriter.Flush();
        }

        private void openWaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            simpleSound.Stop();

            OpenFileDialog open = new OpenFileDialog();
            open.Filter = "Wave File (*.wav)|*.wav";
            if (open.ShowDialog() != DialogResult.OK) return;

            customWaveViewer1.WaveStream = new NAudio.Wave.WaveFileReader(open.FileName);
            customWaveViewer1.FitToScreen();

            simpleSound = new SoundPlayer(open.FileName);
            simpleSound.Play();
        }


        private void Initialize_Spectrogram()
        {

            //comboBox1.SelectedIndex = 0;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;

            // konfiguracja karty dzwiekowej 
            rate = 44100;
            buffer_update_hz = 20;
            pixelsPerBuffer = 10;

            // konfiguracja FFT/spectrogramu
            unanalyzed_max_sec = 2.5;
            fft_size = 4096; // musi byc wielokrotnoscia 2
            spec_height = fft_size / 6;

            // wypelnia spektogram pustymi danymi
            spec_data = new List<List<double>>();
            List<double> data_empty = new List<double>();
            for (int i = 0; i < spec_height; i++) data_empty.Add(0);
            for (int i = 0; i < spec_width; i++) spec_data.Add(data_empty);

            // resize picturebox to accomodate data shape
            pictureBox1.Width = spec_data.Count;
            pictureBox1.Height = spec_data[0].Count;
            pictureBox1.Location = new Point(11, 104);

            // zaczyna sluchanie
            var waveIn = new WaveIn();
            waveIn.DeviceNumber = 0;  // jeśli program się wywala należy zmienić ten parametr na 1
            waveIn.DataAvailable += Audio_buffer_captured;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(rate, 1);
            waveIn.BufferMilliseconds = 1000 / buffer_update_hz;
            waveIn.StartRecording();

            timer1.Enabled = true;
        }


        void Analyze_values()
        {

            if (fft_size == 0) return;
            if (unanalyzed_values.Count < fft_size) return;
            while (unanalyzed_values.Count >= fft_size) Analyze_chunk();
        }


        void Analyze_chunk()
        {

            short[] data = new short[fft_size];
            data = unanalyzed_values.GetRange(0, fft_size).ToArray();

            // usun kolumne najbardziej po lewo 
            spec_data.RemoveAt(0);

            // dodaj nowa kolumne po prawo
            List<double> new_data = new List<double>();


            // przygotuj dane do FFT 
            Complex[] fft_buffer = new Complex[fft_size];
            for (int i = 0; i < fft_size; i++)
            {
                fft_buffer[i].X = (float)(unanalyzed_values[i] * FastFourierTransform.HammingWindow(i, fft_size));
                fft_buffer[i].Y = 0;
            }

            // FFT
            FastFourierTransform.FFT(true, (int)Math.Log(fft_size, 2.0), fft_buffer);

            // wypelnij liste wartosciami fft 
            for (int i = 0; i < spec_data[spec_data.Count - 1].Count; i++)
            {

                double val;
                val = (double)fft_buffer[i].X + (double)fft_buffer[i].Y;
                val = Math.Abs(val);
                if (checkBox1.Checked) val = Math.Log(val);
                new_data.Add(val);
            }

            new_data.Reverse();
            spec_data.Insert(spec_data.Count, new_data); 

            // usun nieanalizowane dane 
            unanalyzed_values.RemoveRange(0, fft_size / pixelsPerBuffer);

        }

        void Update_bitmap_with_data()
        {
            

            // tworzenie bitmapy
            Bitmap bitmap = new Bitmap(spec_data.Count, spec_data[0].Count, PixelFormat.Format8bppIndexed);

            ColorPalette pal = bitmap.Palette;

 

                for (int i = 0; i < 256; i++) {

                

                        pal.Entries[i] = Color.FromArgb(255, i, i, i);

                 
                }

            
            bitmap.Palette = pal;

            // przygotuj dostep do danych przez bitmapdata 
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                    ImageLockMode.ReadOnly, bitmap.PixelFormat);

            // swtorz tablite byte by odwzorowac pixele na obrazie 
            byte[] pixels = new byte[bitmapData.Stride * bitmap.Height];

            // wypelnij tablice danymi 
            for (int col = 0; col < spec_data.Count; col++)
            {

                double scaleFactor;
                scaleFactor = (double)numericUpDown1.Value;

                for (int row = 0; row < spec_data[col].Count; row++)
                {
                    int bytePosition = row * bitmapData.Stride + col;
                    double pixelVal = spec_data[col][row] * scaleFactor;
                    pixelVal = Math.Max(0, pixelVal);
                    pixelVal = Math.Min(255, pixelVal);
                    pixels[bytePosition] = (byte)(pixelVal);
                }
            }

            // zamien tablice na bitmape 
            Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
            bitmap.UnlockBits(bitmapData);

            // wstaw bitmape na picturebox
            pictureBox1.Image = bitmap;
        }


        void Audio_buffer_captured(object sender, WaveInEventArgs args)
        {
            buffers_captured += 1;
            buffers_remaining += 1;

            // interpretacja jako 16bit
            short[] values = new short[args.Buffer.Length / 2];
            for (int i = 0; i < args.BytesRecorded; i += 2)
            {
                values[i / 2] = (short)((args.Buffer[i + 1] << 8) | args.Buffer[i + 0]);
            }

            // dodanie wartosci do listy
            unanalyzed_values.AddRange(values);

            int unanalyzed_max_count = (int)unanalyzed_max_sec * rate;

            if (unanalyzed_values.Count > unanalyzed_max_count)
            {
                unanalyzed_values.RemoveRange(0, unanalyzed_values.Count - unanalyzed_max_count);
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Analyze_values();
            Update_bitmap_with_data();
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void zakończToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
