using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ComponentModel;

namespace PeekPoker
{
    /// <summary>Contains function/s that deals with I/O reading and writing of Data</summary>
    public class RWStream
    {
        #region Eventhandlers/DelegateHandlers
        public event UpdateProgressBarHandler ReportProgress;
        #endregion

        private bool _accessed;
        private BinaryReader _bReader;
        private BinaryWriter _bWriter;
        private string _fileName;
        private Stream _fStream;
        private readonly bool _isBigEndian;

        #region RwStream Constructors
        /// <summary>Makes a temporary file Stream</summary>
        public RWStream()
        {
            try
            {
                _fileName = Path.GetTempPath() + Guid.NewGuid() + ".ISOLib";
                _fStream = new FileStream(_fileName,FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                _bReader = new BinaryReader(_fStream);
                _bWriter = new BinaryWriter(_fStream);
                _isBigEndian = true;
                _accessed = true;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        #endregion

        #region Methods
        /// <summary>Clears buffer by Flushing and Closes theI/O Stream</summary>
        public void Close()
        {
            try
            {
                if (_accessed)
                {
                    Flush();
                    _bWriter.Close();
                    _bWriter = null;
                    _bReader.Close();
                    _bReader = null;
                    _fStream.Close();
                    _fStream = null;
                    _accessed = false;
                    if (_fileName != null)
                    {
                        File.Delete(_fileName);
                        _fileName = null;
                    }
                        Dispose();
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }
        /// <summary>Release all resources used by reader and writer stream</summary>
        private void Dispose()
        {
            try
            {
                if (_accessed)
                {
                    if (_bReader != null)
                    {
                        _bReader.BaseStream.Dispose();
                    }
                    if (_bWriter != null)
                    {
                        _bWriter.BaseStream.Dispose();
                    }
                }
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }
        /// <summary>Clears buffer for this stream and any unbuffered data will be written</summary>
        public void Flush()
        {
            try
            {
                _bReader.BaseStream.Flush();
                _bWriter.BaseStream.Flush();
                _fStream.Flush();
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }
        #endregion

        #region Reader
        /// <summary>Reads a set size of bytes</summary>
        /// <param name="length">The byte array length</param>
        /// <returns>Byte Array</returns>
        public byte[] ReadBytes(int length)
        {
            return ReadBytes(length, _isBigEndian);
        }

        /// <summary>Reads a set size of bytes</summary>
        /// <param name="length">The byte array length</param>
        /// <param name="isBigEndien">Specifiy if read is in Big Endien Type</param>
        private byte[] ReadBytes(int length, bool isBigEndien)
        {
            try
            {
                if (Position == Length)
                    throw new Exception("Cannot move position past file size");
                if (length == 0)
                    return new byte[0];
                var buffer = new byte[length];
                _fStream.Read(buffer, 0, length);
                if(!isBigEndien)
                    Array.Reverse(buffer);
                return buffer;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        /// <summary>Search the file stream for a specified Hex String</summary>
        /// <param name="data">The data you want to search for</param>
        /// <param name="firstFind">If you want to only search for the first location</param>
        /// <returns>The long offset</returns>
        public long[] SearchHexString(string data, bool firstFind)
        {
            try
            {
                //location of the value/s
                var locations = new List<long>();
                long index;
                int prev = (int)Position;
                for (index = Position; index < Length; index++)
                {
                    Position = index;

                    if (!firstFind)
                        ReportProgress(prev, (int)Length, (int)index, "Searching...");

                    var buffer = ReadBytes(data.Length/2, _isBigEndian);
                    if (data != Functions.ToHexString(buffer)) continue;
                    locations.Add(index);

                    if (firstFind)break;
                }
                return locations.ToArray();
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }
        public List<Types.SearchResults> ExSearchHexString(string data, uint _startDumpOffset, bool firstFind)
        {
            try
            {
                //location of the value/s
                List<Types.SearchResults> locations = new List<Types.SearchResults>();
                long index;
                int prev = (int)Position;
                byte[] _buffer = ReadBytes((int)Length, _isBigEndian);
                Position = prev;

                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

                for (index = Position; index < Length; index = Position)
                {
                    if (!firstFind)
                        ReportProgress(prev, (int)Length, (int)index, "Searching...");

                    byte _value = Functions.HexToBytes(data.Substring(0, 2))[0];
                    int ind = (Array.IndexOf(_buffer, _value, (int)Position));

                    if (ind == -1)
                    {
                        break;
                    }
                    else
                    {
                        Position = ind;

                        string buffer = Functions.ToHexString(ReadBytes(data.Length / 2, _isBigEndian));
                        if (data != buffer)
                        {
                            Position = ind + 1;
                            continue;
                        }
                        Types.SearchResults results = new Types.SearchResults();
                        results.Offset = Functions.ToHexString(Functions.UInt32ToBytes(_startDumpOffset + (uint)ind));
                        results.Value = buffer;

                        locations.Add(results);
                        if (firstFind) break;
                    }
                    index = Position;
                }
                watch.Stop();
                Console.WriteLine("Parallel for - Search: Seconds Elapsed: " + watch.Elapsed.Seconds);
                return locations;
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }
        public BindingList<Types.SearchResults> Ex2SearchHexString(byte[] pattern, uint _startDumpOffset)
        {
            byte[] buffer = ReadBytes((int)Length, _isBigEndian);
            BindingList<Types.SearchResults> positions = new BindingList<Types.SearchResults>();
            int startIndex = 0;
            int i = Array.IndexOf<byte>(buffer, pattern[0], startIndex);
            int x = 1;
            while (i >= 0 && i <= buffer.Length - pattern.Length)
            {
                ReportProgress(0, (int)buffer.Length, (int)i, "Searching...");

                byte[] segment = new byte[pattern.Length];
                Buffer.BlockCopy(buffer, i, segment, 0, pattern.Length);
                if (segment.SequenceEqual<byte>(pattern))
                {
                    Types.SearchResults results = new Types.SearchResults();
                    results.Offset = Functions.ToHexString(Functions.UInt32ToBytes(_startDumpOffset + (uint)i));
                    results.Value = Functions.ByteArrayToString(segment);
                    results.ID = x.ToString();
                    positions.Add(results);
                    i = Array.IndexOf<byte>(buffer, pattern[0], i + pattern.Length);
                    x++;
                }
                else 
                {
                    i = Array.IndexOf<byte>(buffer, pattern[0], i + 1);
                }
                

            }
            return positions;
        }
        #endregion

        #region Writer
        /// <summary>Writes a region of a byte array to the current stream</summary>
        /// <param name="buffer">A byte array containing the data to write</param>
        /// <param name="index">The starting point to start writing</param>
        /// <param name="count">The amount of bytes to write</param>
        public void WriteBytes(byte[] buffer, int index, int count)
        {
            try
            {
                WriteBytes(buffer, index, count, _isBigEndian);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        /// <summary>Writes a region of a byte array to the current stream</summary>
        /// <param name="buffer">A byte array containing the data to write</param>
        /// <param name="index">The starting point to start writing</param>
        /// <param name="count">The amount of bytes to write</param>
        /// <param name="isBigEndian">If the Endien type is Big</param>
        private void WriteBytes(byte[] buffer, int index, int count, bool isBigEndian)
        {
            try
            {
                if (!isBigEndian)
                    Array.Reverse(buffer);
                _bWriter.Write(buffer, index, count);
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }
        #endregion
        
        #region Properties
        /// <summary>Get/Set if current initialization length</summary>
        private long Length
        {
            get
            {
                return _fStream.Length;
            }
        }
        /// <summary>Get/Set if current initialization position</summary>
        public long Position
        {
            private get
            {
                return _fStream.Position;
            }
            set
            {
                try
                {
                    if ((value != _fStream.Position) || (value <= _fStream.Length))
                    {
                        _fStream.Position = value;
                    }
                }
                catch (Exception exception)
                {
                    throw new Exception(exception.Message);
                }
            }
        }
        #endregion
    }
}

