// <copyright file="Press.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DynamicSymbols.Machines
{
    using System;
    using System.Threading;

    internal class Press : Machine
    {
        private double maxPressure = 100;
        private TimeSpan compressionTime = new TimeSpan(0, 0, 10);

        public override string Description
        {
            get
            {
                return "Presses different materials into one";
            }
        }

        public double CurrentPressure { get; private set; } = 0;

        public double MaxPressure
        {
            get
            {
                return maxPressure;
            }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Concat("Maximum pressure must not be less than 0 bar."));

                maxPressure = value;
            }
        }

        public TimeSpan CompressionTime
        {
            get
            {
                return compressionTime;
            }
            set
            {
                if (value < TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Concat("Compression time must not be less than zero."));

                compressionTime = value;
            }
        }

        private void Compress()
        {
            while ((CurrentPressure < MaxPressure) && (!HasError))
            {
                CurrentPressure++;
                Thread.Sleep(100);
            }

            if (CurrentPressure == MaxPressure)
                Thread.Sleep(CompressionTime);

            CurrentPressure = 0;
            CompleteWork();
        }

        protected override void DoWork(object state)
        {
            Compress();
        }
    }
}
