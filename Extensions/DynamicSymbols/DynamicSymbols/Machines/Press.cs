// <copyright file="Press.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;

namespace DynamicSymbols.Machines
{
    internal class Press : Machine
    {
        private TimeSpan _compressionTime = new TimeSpan(0, 0, 10);
        private double _maxPressure = 100;

        public override string Description
        {
            get
            {
                return "Squeezes different materials into one";
            }
        }

        public double CurrentPressure { get; private set; }

        public double MaxPressure
        {
            get
            {
                return _maxPressure;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        string.Concat("Maximum pressure must not be less than 0 bar."));
                }

                _maxPressure = value;
            }
        }

        public TimeSpan CompressionTime
        {
            get
            {
                return _compressionTime;
            }
            set
            {
                if (value < TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        string.Concat("Compression time must not be less than zero."));
                }

                _compressionTime = value;
            }
        }

        private void Compress()
        {
            while (CurrentPressure < MaxPressure && !HasError)
            {
                CurrentPressure++;
                Thread.Sleep(100);
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (CurrentPressure == MaxPressure)
            {
                Thread.Sleep(CompressionTime);
            }

            CurrentPressure = 0;
            CompleteWork();
        }

        protected override void DoWork(object state)
        {
            Compress();
        }
    }
}
