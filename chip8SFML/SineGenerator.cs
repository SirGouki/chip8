using System;
using System.Collections.Generic;
using System.Text;

namespace chip8SFML
{
    class SineGenerator
    {
        private readonly double _frequency;
        private readonly UInt32 _sampleRate;
        private readonly UInt16 _secondsInLength;
        private short[] _dataBuffer;

        public short[] Data { get { return _dataBuffer; } }

        public SineGenerator(double freq, UInt32 sampleRate, UInt16 seconds)
        {
            _frequency = freq;
            _sampleRate = sampleRate;
            _secondsInLength = seconds;
            GenerateData();
        }

        private void GenerateData()
        {
            uint bufferSize = _sampleRate * _secondsInLength;
            _dataBuffer = new short[bufferSize];

            int amplitude = 32760;

            double timePeriod = (Math.PI * 2 * _frequency) / (_sampleRate);

            for(uint i = 0; i < bufferSize - 1; i++)
            {
                _dataBuffer[i] = Convert.ToInt16(amplitude * Math.Sin(timePeriod * i));
            }
        }
    }
}
