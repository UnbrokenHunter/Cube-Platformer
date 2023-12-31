using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Player {
    public class PlayerVisuals : MonoBehaviour
    {
        [Title("General")]
        [SerializeField] private bool _doVisuals = false;

        [Title("Wall")]
        [SerializeField] private ParticleSystem _wallSlideParticle;

        [Title("Dash")]
        [SerializeField] private ParticleSystem _dashParticle;

        public void WallSlideEffect(float direction)
        {
             if (!_doVisuals) return;

            _wallSlideParticle.transform.localScale = new Vector3(direction, 
                _wallSlideParticle.transform.localScale.y, 
                _wallSlideParticle.transform.localScale.z);

            _wallSlideParticle.Play();
        }
        public void DashEffect(Vector2 direction)
        {
            if (!_doVisuals) return;


        }

        public void DashEffectContinual()
        {
            if (!_doVisuals) return;

            _dashParticle.Play();
        }
    }
}
