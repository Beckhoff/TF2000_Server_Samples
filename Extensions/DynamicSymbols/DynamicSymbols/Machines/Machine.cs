//-----------------------------------------------------------------------
// <copyright file="Machine.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace DynamicSymbols.Machines
{
    using System.Threading;

    internal abstract class Machine
    {
        private readonly object stateLock = new object();

        public abstract string Description { get; }

        public MachineState State { get; protected set; } = MachineState.Idle;

        public bool IsWorking
        {
            get
            {
                lock (stateLock)
                {
                    return State == MachineState.Working;
                }
            }
            set
            {
                if (value)
                {
                    lock (stateLock)
                    {
                        if (State == MachineState.Idle)
                        {
                            if (ThreadPool.QueueUserWorkItem(DoWork))
                                State = MachineState.Working;
                        }
                    }
                }
            }
        }

        public bool HasError
        {
            get
            {
                lock (stateLock)
                {
                    return State == MachineState.Error;
                }
            }
            set
            {
                if (value)
                {
                    lock (stateLock)
                    {
                        State = MachineState.Error;
                    }
                }
                else
                {
                    lock (stateLock)
                    {
                        if (State == MachineState.Error)
                            State = MachineState.Idle;
                    }
                }
            }
        }

        protected abstract void DoWork(object state);

        protected void CompleteWork()
        {
            lock (stateLock)
            {
                if (State == MachineState.Working)
                    State = MachineState.Idle;
            }
        }
    }
}
