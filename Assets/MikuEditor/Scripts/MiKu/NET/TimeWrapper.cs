using System;
using System.Collections.Generic;
using MiKu.NET.Charting;


namespace MiKu.NET {
    public sealed class TimeWrapper : IComparable, IEqualityComparer<TimeWrapper> {
        public TimeWrapper(float value) {
            if(Divisor != 1)
                Divisor = ((Track.BPM/60f)/64f)*50;
            this.FloatValue = value;
        }
        public TimeWrapper() { }
        public static TimeWrapper Create(float value) { return new TimeWrapper(value); }

        public float FloatValue
        {
            get
            {
                return _value;
            }

            set
            {
                Hash = (int)(Math.Round(value/Divisor, 0, MidpointRounding.AwayFromZero));
                _pureHash =Hash;
                _value = value;
            }
        }
        public static float Divisor
        {
            get
            {
                return _divisor;
            }

            set
            {
                _divisor = value;
            }
        }

        public int Hash
        {
            get
            {
                return _hash;
            }

            set
            {
                _hash = value;
            }
        }

        float _value = 0;
        static float _divisor = 1;
        int _hash = 1;
        int _pureHash = 1;

        public void RegenerateHash() {
            Divisor = ((Track.BPM/60f)/64f)*50;
            Hash = (int)(Math.Round(FloatValue/Divisor, 0, MidpointRounding.AwayFromZero));
        }

        public static int GetPreciseInt(TimeWrapper f) {
            return f.Hash;
            //Trace.WriteLine("Created hash source:" + result.whole + " divisor: " + divisor + " oroginal: " + f.FloatValue);
        }

        public int CompareTo(object obj) {
            TimeWrapper objectAsTimeWrapper = obj as TimeWrapper;
            if(this.GetHashCode() == objectAsTimeWrapper.GetHashCode())
                return 0;
            if(this.FloatValue < objectAsTimeWrapper.FloatValue)
                return -1;
            if(this.FloatValue > objectAsTimeWrapper.FloatValue)
                return 1;
            return -1;
        }

        public override bool Equals(object obj) {
            TimeWrapper objectAsTimeWrapper = obj as TimeWrapper;

            if(objectAsTimeWrapper == null) {
                return false;
            }

            return this == objectAsTimeWrapper;
        }

        public bool Equals(TimeWrapper f1, TimeWrapper f2) {
            return GetPreciseInt(f1) == GetPreciseInt(f2);
        }

        public int GetHashCode(TimeWrapper f) {
            return GetPreciseInt(f).GetHashCode();
        }
        public override int GetHashCode() {
            return GetPreciseInt(this).GetHashCode();
        }
        public static bool EqualsTo(TimeWrapper f1, TimeWrapper f2) {
            int value1 = GetPreciseInt(f1);
            int value2 = GetPreciseInt(f2);
            //Trace.WriteLine("Comparing times: " + f1.FloatValue + " and " + f2.FloatValue);
            //Trace.WriteLine("Comparing integers: " + value1 + " and " + value2);
            return value1 == value2;
        }
        public static bool operator ==(TimeWrapper a, TimeWrapper b) {
            if(System.Object.ReferenceEquals(a, null) && System.Object.ReferenceEquals(b, null))
                return true;
            if(System.Object.ReferenceEquals(a, null) || System.Object.ReferenceEquals(b, null))
                return false;
            return EqualsTo(a.FloatValue, b.FloatValue);
        }
        public static bool operator !=(TimeWrapper a, TimeWrapper b) {
            return !(a==b);
        }
        public static bool operator <=(TimeWrapper a, TimeWrapper b) {
            if(System.Object.ReferenceEquals(a, null) || System.Object.ReferenceEquals(b, null))
                return false;
            if(a == b)
                return true;
            return a.FloatValue < b.FloatValue;
        }
        public static bool operator >=(TimeWrapper a, TimeWrapper b) {
            if(System.Object.ReferenceEquals(a, null) || System.Object.ReferenceEquals(b, null))
                return false;
            if(a == b)
                return true;
            return a.FloatValue > b.FloatValue;
        }
        public static TimeWrapper operator +(TimeWrapper a, TimeWrapper b) {
            if(System.Object.ReferenceEquals(a, null) || System.Object.ReferenceEquals(b, null))
                return null;
            return a.FloatValue+b.FloatValue;
        }
        public static TimeWrapper operator -(TimeWrapper a, TimeWrapper b) {
            if(System.Object.ReferenceEquals(a, null) || System.Object.ReferenceEquals(b, null))
                return null;
            return a.FloatValue-b.FloatValue;
        }
        public static bool operator <(TimeWrapper a, TimeWrapper b) {
            if(System.Object.ReferenceEquals(a, null) || System.Object.ReferenceEquals(b, null))
                return false;
            if(a == b)
                return false;
            return a.FloatValue < b.FloatValue;
        }

        public static bool operator >(TimeWrapper a, TimeWrapper b) {
            if(System.Object.ReferenceEquals(a, null) || System.Object.ReferenceEquals(b, null))
                return false;
            if(a == b)
                return false;
            return a.FloatValue > b.FloatValue;
        }
        public static implicit operator TimeWrapper(float value) { return Create(value); }

        public static List<float> Convert(List<TimeWrapper> wrappers) {
            List<float> list = new List<float>();
            foreach(var time in wrappers.OrEmptyIfNull())
                list.Add(time.FloatValue);
            return list;
        }
        public static List<TimeWrapper> Convert(List<float> wrappers) {
            List<TimeWrapper> list = new List<TimeWrapper>();
            foreach(var time in wrappers.OrEmptyIfNull())
                list.Add(time);
            return list;
        }
    }

}