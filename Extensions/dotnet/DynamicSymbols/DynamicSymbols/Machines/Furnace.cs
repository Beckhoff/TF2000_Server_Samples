//-----------------------------------------------------------------------
// <copyright file="Furnace.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Threading;

namespace DynamicSymbols.Machines
{
    internal class Furnace : Machine
    {
        private const int MinTemperature = 20;
        private readonly object _heaterLock = new object();
        private int _maxTemperature = 100;

        public override string Description
        {
            get
            {
                return "Furnace for heating or melting materials";
            }
        }

        public int CurrentTemperature { get; private set; } = MinTemperature;

        public int MaxTemperature
        {
            get
            {
                return _maxTemperature;
            }
            set
            {
                if (value < MinTemperature)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        string.Concat("Maximum temperature must not be less than ", MinTemperature, "°C."));
                }

                _maxTemperature = value;
            }
        }

        private void HeatUp()
        {
            lock (_heaterLock)
            {
                while (CurrentTemperature < MaxTemperature && !HasError)
                {
                    CurrentTemperature++;
                    Thread.Sleep(100);
                }

                if (CurrentTemperature == MaxTemperature)
                {
                    Thread.Sleep(10000);
                }
            }

            CompleteWork();
            CoolDown();
        }

        private void CoolDown()
        {
            lock (_heaterLock)
            {
                while (CurrentTemperature > MinTemperature)
                {
                    if (IsWorking)
                    {
                        break;
                    }

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
