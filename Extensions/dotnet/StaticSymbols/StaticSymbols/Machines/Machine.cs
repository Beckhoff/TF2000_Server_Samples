//-----------------------------------------------------------------------
// <copyright file="Machine.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

using System.Threading;

namespace StaticSymbols.Machines
{
    internal abstract class Machine
    {
        private readonly object _stateLock = new object();

        public abstract string Description { get; }

        public MachineState State { get; private set; } = MachineState.Idle;

        public bool IsWorking
        {
            get
            {
                lock (_stateLock)
                {
                    return State == MachineState.Working;
                }
            }
            set
            {
                if (value)
                {
                    lock (_stateLock)
                    {
                        if (State == MachineState.Idle)
                        {
                            if (ThreadPool.QueueUserWorkItem(DoWork))
                            {
                                State = MachineState.Working;
                            }
                        }
                    }
                }
            }
        }

        public bool HasError
        {
            get
            {
                lock (_stateLock)
                {
                    return State == MachineState.Error;
                }
            }
            set
            {
                if (value)
                {
                    lock (_stateLock)
                    {
                        State = MachineState.Error;
                    }
                }
                else
                {
                    lock (_stateLock)
                    {
                        if (State == MachineState.Error)
                        {
                            State = MachineState.Idle;
                        }
                    }
                }
            }
        }

        protected abstract void DoWork(object state);

        protected void CompleteWork()
        {
            lock (_stateLock)
            {
                if (State == MachineState.Working)
                {
                    State = MachineState.Idle;
                }
            }
        }
    }
}
