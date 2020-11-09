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
        private int maxRandom = 1000;
        private object maxRandomLock = new object();

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
    }
}
