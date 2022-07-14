//-----------------------------------------------------------------------
// <copyright file="Saw.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Threading;

namespace DynamicSymbols.Machines
{
    internal class Saw : Machine
    {
        public override string Description
        {
            get
            {
                return "Saws a workpiece into two or more parts";
            }
        }

        public uint CurrentRotationsPerMinute { get; private set; }

        public uint MaxRotationsPerMinute { get; set; } = 3000;

        public uint NumberOfPieces { get; set; } = 2;

        private void SawUp()
        {
            var maxRotationsPerMinute = MaxRotationsPerMinute;
            var numberOfPieces = NumberOfPieces;

            for (uint i = 1; i < numberOfPieces; i++)
            {
                while (CurrentRotationsPerMinute < maxRotationsPerMinute && !HasError)
                {
                    CurrentRotationsPerMinute++;
                    Thread.Sleep(1);
                }

                if (CurrentRotationsPerMinute == maxRotationsPerMinute)
                {
                    Thread.Sleep(5000);
                }

                while (CurrentRotationsPerMinute > 0)
                {
                    CurrentRotationsPerMinute--;
                    Thread.Sleep(1);
                }
            }

            CompleteWork();
        }

        protected override void DoWork(object state)
        {
            SawUp();
        }
    }
}
