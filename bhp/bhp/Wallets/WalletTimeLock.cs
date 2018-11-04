using System; 

namespace Bhp.Network.RPC
{
    public class WalletTimeLock
    {
        private int Duration = 1; // minutes 
        private DateTime UnLockTime;
        private string password; 

        public WalletTimeLock(string password)
        {
            UnLockTime = DateTime.Now;
            Duration = 0;
            this.password = password;
        }

        public void SetPassword(string password)
        {
            this.password = password;
        }

        /// <summary>
        /// Unlock wallet
        /// </summary>
        /// <param name="Duration">Unlock duration</param>
        public bool UnLock(string password,int duration)
        {
            lock (this)
            {
                if (this.password.Length > 0 && this.password.Equals(password))
                {
                    Duration = duration > 1 ? duration : 1;
                    UnLockTime = DateTime.Now;
                    return true;
                }
                else
                {
                    return false;
                }               
            }
        }

        public bool IsLocked()
        {
            lock (this)
            {
                TimeSpan span = new TimeSpan(DateTime.Now.Ticks) - new TimeSpan(UnLockTime.Ticks);
                return ((int)span.TotalMinutes >= Duration);
            }
        }
    }
}
