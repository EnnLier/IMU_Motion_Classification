using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BLE_Drive_UI.Domain
{
    public class StringBuffer
    {
        private static UInt16 _bufferLength;
        private static String[] SBuffer;

        public UInt16 Count { get; private set; }
        public bool Active { get; set; }

        public StringBuffer(UInt16 bufferlength)
        {
            _bufferLength = bufferlength;

            SBuffer = new String[_bufferLength];

            Count = 0;
        }

        public void Add(String data)
        {
            if (Count <= _bufferLength)
            {
                SBuffer[Count] = data;
                Count++;
            }
            else
            {
                throw new Exception("StringBuffer Overflow");
            }
        }

        public String[] Flush()
        {
            var tmp = new String[_bufferLength];
            SBuffer.CopyTo(tmp, 0);
            //Console.WriteLine(tmp[0]);
            Clear();
            Count = 0;
            return tmp;
        }

        public void Clear()
        {
            for (int i = 0; i < Count; i++)
            {
                SBuffer[i] = String.Empty;
            }
        }
    }

 
    public class SyncDataSaver
    {
        private static String _path;
        private static UInt16 _bufferLength;

        private StringBuffer buffer1;
        private StringBuffer buffer2;

        private static object mThreadLock = new object();

        private Thread Buffer1PollingThread;
        private Thread Buffer2PollingThread;
        private Stopwatch Watch;

        private static double _rate;
        private double cummulativeRate;

        private String dataToSave = String.Empty;
        private String timeStamp = String.Empty;

        public bool isSaving = false;

        public bool Active = false;

        public SyncDataSaver(UInt16 bufferLength, double rate)
        {

            var dir = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName;
            Console.WriteLine(dir);

            _path = dir + @"\Data\";

            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            _bufferLength = bufferLength;
            _rate = rate;

            buffer1 = new StringBuffer(_bufferLength);
            buffer2 = new StringBuffer(_bufferLength);

            buffer1.Active = true;
            buffer2.Active = false;

            Watch = new Stopwatch();

        }

        public void start()
        {
            Console.WriteLine("Start Saving....");
            waitForThreadsToFinish();
            dataToSave = String.Empty;
            Active = true;
            isSaving = true;
            buffer1.Active = true;
            buffer2.Active = false;

            cummulativeRate = 0;

            Buffer1PollingThread = new Thread(Buffer1Polling);
            //Buffer2PollingThread = new Thread(Buffer2Polling);

            Buffer1PollingThread.Start();
            //Buffer2PollingThread.Start();



            Console.WriteLine("PollingThread started....");
        }

        public void stop()
        {
            isSaving = false;
            //dataToSave = String.Empty;
            flush();
            Watch.Reset();
            waitForThreadsToFinish();
            Active = false;
        }

        public void addData(int id, float[] data)
        {
            String stringToSave = id.ToString();
            foreach(var value in data)
            {
                stringToSave += value.ToString("0.0000");
                //for (int i = 0; i < value.Length ; i++)
                //{
                //    Console.WriteLine(i);
                //    stringToSave += value[i].ToString("0.0000");
                //}
                    
            }
            addData(stringToSave);
        }

        public void addData(String data)
        {
            lock (mThreadLock)
            {
                dataToSave = data;
            }
        }

        private void Buffer1Polling()
        {
            while (dataToSave == String.Empty) { Thread.Sleep(1); }
            Watch.Start();
            while (isSaving)
            {
                //if (!buffer1.Active) { var n = _rate / 5; Thread.Sleep((int)n); continue; }
                double t = Watch.Elapsed.TotalMilliseconds;
                if (t >= cummulativeRate)
                {
                    cummulativeRate += _rate;
                    String timestamp = (t / 1000).ToString("0.0000");
                    lock (mThreadLock)
                    {
                        buffer1.Add(timestamp + dataToSave);
                        Console.WriteLine(buffer1.Count);
                    }
                    if (buffer1.Count >= _bufferLength)
                    {
                        buffer1.Active = false;
                        buffer2.Active = true;
                        Buffer2PollingThread = new Thread(Buffer2Polling);
                        Buffer2PollingThread.Start();
                        save(buffer1);
                        return;
                    }
                }
            }
        }

        private void Buffer2Polling()
        {
            while (isSaving)
            {
                //if (!buffer2.Active){ var n = _rate / 5; Thread.Sleep((int)n); continue; }
                double t = Watch.Elapsed.TotalMilliseconds;
                if (t >= cummulativeRate)
                {
                    cummulativeRate += _rate;
                    String timestamp = (t / 1000).ToString("0.0000");
                    lock (mThreadLock)
                    {
                        buffer2.Add(timestamp + dataToSave);
                    }
                    if (buffer2.Count >= _bufferLength)
                    {
                        buffer1.Active = true;
                        buffer2.Active = false;
                        Buffer1PollingThread = new Thread(Buffer1Polling);
                        Buffer1PollingThread.Start();
                        save(buffer2);
                        return;
                    }
                }
            }
        }

        private bool waitForThreadsToFinish()
        {
            try
            {
                if (Buffer1PollingThread != null)
                {
                    while (Buffer1PollingThread.IsAlive)
                    {
                        Console.WriteLine(" Threads still alive....");
                        Thread.Sleep(200);
                    }
                }
                if (Buffer2PollingThread != null)
                {
                    while (Buffer2PollingThread.IsAlive)
                    {
                        Console.WriteLine(" Threads still alive....");
                        Thread.Sleep(200);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed while waiting for pollingthreads to finish: " + e.Message);
                return false;
            }
            return true;
        }

        private bool busy = false;
        private void save(StringBuffer buffer)
        {
            busy = true;
            String filename = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            File.WriteAllLines(_path + filename + ".txt", buffer.Flush());
            busy = false;
        }

        private void flush()
        {
            while (busy) { Thread.Sleep(20); }
            if (buffer1.Active)
            {
                save(buffer1);
            }
            else if (buffer2.Active)
            {
                save(buffer2);
            }
            //while (SavingThread.IsAlive) { }
            //SavingThread.Start();
        }

        
    }
    public class DoubleDataBufferAsync
    {
        //private float[][] fBuffer1;
        //private float[][] fBuffer2;

        //private static List<String> sBuffer1 = new List<String>();
        //private static List<String> sBuffer2 = new List<String>();
        private static bool _oneActive = true;
        private static bool _twoActive = false;

        private static String _path;
        private static UInt16 _bufferLength;
        //private static UInt16 _dataPoints;

        //private UInt16 _fBufC1;
        //private UInt16 _fBufC2;
        private static UInt16 _sBufC1;
        private static UInt16 _sBufC2;

        private static String[] sBuffer1;
        private static String[] sBuffer2;

        private static object mThreadLock = new object();

        public DoubleDataBufferAsync(UInt16 bufferLength)//, UInt16 dataPoints)
        {
            var dir = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName;
            Console.WriteLine(dir);

            _path = dir + @"\Data\";

            if (!Directory.Exists(_path))
                Directory.CreateDirectory(_path);

            //_dataPoints = dataPoints;
            _bufferLength = bufferLength;
            _oneActive = true;
            _twoActive = false;

            //_fBufC1 = 0;
            //_fBufC2 = 0;

            _sBufC1 = 0;
            _sBufC2 = 0;

            //fBuffer1 = new float[_bufferLength][];
            //fBuffer2 = new float[_bufferLength][];

            sBuffer1 = new String[_bufferLength];
            sBuffer2 = new String[_bufferLength];
        }

        //public async void addData(float[] data)
        //{
        //    if (_oneActive) // Buffer 1 active
        //    {
        //        //Console.WriteLine(Buffer1.Count);
        //        fBuffer1[_fBufC1] = data;
        //        _fBufC1++;
        //        if (_fBufC1 >= _bufferLength)
        //        {
        //            await save();
        //            _twoActive = true;
        //            _oneActive = false;
        //            _fBufC1 = 0;
        //        }
        //    }
        //    else if (_twoActive)
        //    {
        //        fBuffer2[_fBufC2] = data;
        //      _fBufC2++;
        //        if (_fBufC2 >= _bufferLength)
        //        {
        //            await save();
        //            _oneActive = true;
        //            _twoActive = false;
        //            _fBufC2 = 0;
        //        }
        //    }
        //}

        public async void addData(String data)
        {
            var t = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fffffff");
            String toSave = t + " " + data; // + "\n";

            if (_oneActive) // Buffer 1 active
            {
                //Console.WriteLine(Buffer1.Count);
                sBuffer1[_sBufC1] = toSave;
                _sBufC1++;
                if (_sBufC1 >= _bufferLength)
                {
                    _twoActive = true;
                    _oneActive = false;
                    _sBufC1 = 0;
                    await save(sBuffer1);
                }
            }
            else if (_twoActive)
            {
                sBuffer2[_sBufC2] = toSave;
                _sBufC2++;
                if (_sBufC2 >= _bufferLength)
                {
                    _oneActive = true;
                    _twoActive = false;
                    _sBufC2 = 0;
                    await save(sBuffer2);
                }
            }
        }

        static async Task save(String[] buffer)
        {
            //Console.WriteLine("Save from " + Thread.CurrentThread.ManagedThreadId);
            String name = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");
            await Task.Run(() => {
                File.WriteAllLines(_path + name + ".txt", buffer);
            });
            buffer = new String[] { };
        }

        static async Task save()
        {
            if (_oneActive)
            {
                await save(sBuffer1);
            }
            else if (_twoActive)
            {
                await save(sBuffer2);
            }
            _oneActive = true;
            _twoActive = false;
            _sBufC2 = 0;
            _sBufC1 = 0;
        }


        public async void flush()
        {
            await save();
        }
    }
}
