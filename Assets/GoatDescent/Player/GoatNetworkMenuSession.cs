using UnityEngine;

namespace GoatDescent
{
    public abstract class GoatNetworkMenuSession : MonoBehaviour
    {
        public abstract string Status { get; }
        public abstract string Address { get; }
        public abstract bool Busy { get; }
        public abstract string RoomCode { get; }
        public abstract void Host();
        public abstract void Join(string address);
        public abstract void Disconnect();
    }
}
