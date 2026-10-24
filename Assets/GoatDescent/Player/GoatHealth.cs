using UnityEngine;

namespace GoatDescent
{
    [DisallowMultipleComponent]
    public sealed class GoatHealth : MonoBehaviour
    {
        public const float Maximum=100f;
        [SerializeField,Range(0f,Maximum)] private float current=Maximum;
        public float Current => current;
        public float Fraction => current/Maximum;
        public void Damage(float amount)
        {
            if(!MountainAuthority.IsHost || amount<=0f) return;
            current=Mathf.Max(0f,current-amount);
            if(current<=0f) GetComponent<RespawnController>()?.Kill();
        }
        public void HealFully() {if(MountainAuthority.IsHost) current=Maximum;}
        public void Heal(float amount)
        {
            if(MountainAuthority.IsHost && GetComponent<RespawnController>()?.IsDead!=true)
                current=Mathf.Min(Maximum,current+Mathf.Max(0f,amount));
        }
        public void MarkDead() {if(MountainAuthority.IsHost) current=0f;}
        public void ApplyNetwork(float value) {if(!MountainAuthority.IsHost) current=Mathf.Clamp(value,0f,Maximum);}
    }
}
