//-----------------------------------------------------------------------
// <copyright file="Data.cs" company="Beckhoff Automation GmbH & Co. KG">
//     Copyright (c) Beckhoff Automation GmbH & Co. KG. All Rights Reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace RandomValue
{
    // thread-safe class that contains runtime data
    public class Data
    {
        private readonly object _maxRandomLock = new object();
        private int _maxRandom;

        public Data(int maxRandomInit)
        {
            _maxRandom = maxRandomInit;
        }

        public int MaxRandom
        {
            get
            {
                lock (_maxRandomLock)
                {
                    return _maxRandom;
                }
            }

            set
            {
                lock (_maxRandomLock)
                {
                    _maxRandom = value;
                }
            }
        }
    }
}
