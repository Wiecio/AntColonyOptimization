namespace ACO.Shared.Logging
{
    internal static class LogDataFactory
    {
        internal static LogData GetLogData(TimeSpan time, int bestKnown, double error, string instanceName, Result result)
        {
            return result switch
            {
                VRPResult vrpRes => new VRPLogData(time, bestKnown, error, instanceName, vrpRes),
                Result => new LogData(time, bestKnown, error, instanceName, result),
            };
        }
    }

    internal class LogDataBase
    {
        protected readonly string InstanceName;
        protected readonly TimeSpan Time;

        public LogDataBase(string instanceName, TimeSpan time)
        {
            InstanceName = instanceName;
            Time = time;
        }

        public override string ToString()
        {
            return $"{InstanceName};{Time.TotalMilliseconds}";
        }
    }

    internal class LogData : LogDataBase
    {
        private readonly int BestKnown;
        private readonly int Length;
        private readonly double Error;
        private readonly int Iterations;

        public LogData(TimeSpan time, int bestKnown, double error, string instanceName, Result result) : base(instanceName, time)
        {
            Length = result.Length;
            BestKnown = bestKnown;
            Error = error;
            Iterations = result.Iterations;
        }

        public override string ToString()
        {
            return $"{InstanceName};{BestKnown};{Length};{Error};{Iterations};{Time.TotalMilliseconds}";
        }
    }

    internal class VRPLogData : LogData
    {
        private readonly int Vehicles;

        public VRPLogData(TimeSpan time, int bestKnown, double error, string instanceName, VRPResult result) : base(time, bestKnown, error, instanceName, result)
        {
            Vehicles = result.Vehicles;
        }

        public override string ToString()
        {
            return $"{base.ToString()};{Vehicles}";
        }
    }
}