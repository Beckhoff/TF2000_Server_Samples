//-----------------------------------------------------------------------
// <copyright file="Furnace.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DynamicSymbols.Machines
{
    using System;
    using System.Threading;

    internal class Furnace : Machine
    {
        private readonly object heaterLock = new object();

        private const int minTemperature = 20;
        private int maxTemperature = 100;

        public override string Description
        {
            get
            {
                return "Furnace to heating or melting something";
            }
        }

        public int CurrentTemperature { get; private set; } = minTemperature;

        public int MaxTemperature
        {
            get
            {
                return maxTemperature;
            }
            set
            {
                if (value < minTemperature)
                    throw new ArgumentOutOfRangeException(nameof(value), value, string.Concat("Maximum temperature must not be less than ", minTemperature, "°C."));

                maxTemperature = value;
            }
        }

        private void HeatUp()
        {
            lock (heaterLock)
            {
                while ((CurrentTemperature < MaxTemperature) && (!HasError))
                {
                    CurrentTemperature++;
                    Thread.Sleep(100);
                }

                if (CurrentTemperature == MaxTemperature)
                    Thread.Sleep(10000);
            }

            CompleteWork();
            CoolDown();
        }

        private void CoolDown()
        {
            lock (heaterLock)
            {
                while (CurrentTemperature > minTemperature)
                {
                    if (IsWorking)
                        break;

                    CurrentTemperature--;
                    Thread.Sleep(100);
                }
            }
        }

        protected override void DoWork(object state)
        {
            HeatUp();
        }
    }
}
