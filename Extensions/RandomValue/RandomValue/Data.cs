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
        private readonly object maxRandomLock = new object();
        private int maxRandom;

        public int MaxRandom
        {
            get
            {
                lock (this.maxRandomLock)
                {
                    return this.maxRandom;
                }
            }

            set
            {
                lock (this.maxRandomLock)
                {
                    this.maxRandom = value;
                }
            }
        }

        public Data(int maxRandomInit)
        {
            maxRandom = maxRandomInit;
        }
    }
}
