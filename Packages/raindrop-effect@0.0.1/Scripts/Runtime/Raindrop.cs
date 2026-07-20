// Created By: WangYu  Date: 2024-11-18

using UnityEngine;

namespace RaindropEffect
{
    /// <summary>
    /// 雨滴的抽象类
    /// </summary>
    public class Raindrop
    {
        private RaindropSimulator m_simulator; // 模拟器
        
        /// <summary>
        /// 归属的网格
        /// </summary>
        public Grid belongToGrid;
        
        /// <summary>
        /// 父雨滴 - 
        /// 如果当前这个雨滴是别的雨滴拖尾造成的，那么它必有1个父雨滴来描述它2之间的关系
        /// </summary>
        public Raindrop parentRaindrop;
        
        private float m_mass = 0;
        private float m_density = 0;
        private Vector2 m_size = Vector2.zero;
        
        private float m_frictionForce = 0; // 摩擦力
        private float m_xVelocityCoefficient = 0; // x轴速度系数
        private float m_timer; // 计时器
        private float m_nextRandomForceTime = 0; // 下一次随机受力的时间
        
        public Vector2 position;
        public Vector2 velocity;
        public Quaternion rotation = Quaternion.identity;
        
        private Vector2 m_simVelocity; // 模拟速度和真实速度最大的差别是，没有乘方向
        private Vector2 m_sizeSpread;
        
        /// <summary>
        /// 将被回收的标记
        /// </summary>
        public bool willBeDestroyed = false;
        
        private Vector2 m_lastTrailRaindropPos; // 上一个拖尾雨滴的位置
        private float m_nextTrailRaindropDistance; // 下一个拖尾雨滴的距离

        public Raindrop(RaindropSimulator simulator, Vector2 pos, float size, float density = 1)
        {
            m_simulator = simulator;
            position = pos;
            m_density = density;
            
            m_sizeSpread = new Vector2(simulator.simuParas.initSizeSpread, simulator.simuParas.initSizeSpread);
            Mass = (size * density) * (size * density);
        }
        
        /// <summary>
        /// 质量
        /// </summary>
        public float Mass
        {
            get => m_mass;
            set
            {
                m_mass = value;

                float newSize = Mathf.Sqrt(m_mass) / m_density;
                m_size.x = (1 + m_sizeSpread.x) * newSize;
                m_size.y = (1 + m_sizeSpread.y) * newSize;
            }
        }
        
        /// <summary>
        /// 尺寸
        /// </summary>
        public Vector2 Size => m_size;
        
        /// <summary>
        /// 合并的距离
        /// </summary>
        public float GetMergeDistance()
        {
            return Size.x * 0.1f;
        }
        
        /// <summary>
        /// 更新雨滴
        /// </summary>
        public void Update(float deltaTime)
        {
            UpdateForce(deltaTime);
            UpdatePosition(deltaTime);
            UpdateTrail();
        }
        
        /// <summary>
        /// 更新力
        /// </summary>
        public void UpdateForce(float deltaTime)
        {
            if (m_timer >= m_nextRandomForceTime)
            {
                float maxFrictionForce = m_simulator.simuParas.gravity * Mathf.Pow(m_simulator.simuParas.raindropSizeRange[1], 2);
                //float maxFrictionForce = -m_simulator.simuParas.gravity * Mass;
                m_frictionForce = Random.Range(0f, 1f) * maxFrictionForce * m_simulator.simuParas.frictionForceCoefficient;

                float randomXVelocityDir = Random.Range(-1f, 1f);
                float xVelocityCoefficient = Random.Range(m_simulator.simuParas.xVelocityCoefficientRange[0], m_simulator.simuParas.xVelocityCoefficientRange[1]);
                m_xVelocityCoefficient = randomXVelocityDir * xVelocityCoefficient;

                m_nextRandomForceTime = m_timer + Random.Range(m_simulator.simuParas.forceUpdateIntervalRange[0], m_simulator.simuParas.forceUpdateIntervalRange[1]);
            }
            m_timer += deltaTime;
        }
        
        /// <summary>
        /// 更新位置
        /// </summary>
        private void UpdatePosition(float deltaTime)
        {
            Mass -= m_simulator.simuParas.massEvaporateRate * deltaTime;
            float totalForce = m_simulator.simuParas.gravity * Mass - m_frictionForce;
            float acceleration = totalForce / Mass; // 加速度

            m_simVelocity.y -= acceleration * deltaTime;
            m_simVelocity.y = Mathf.Min(0, m_simVelocity.y);
            m_simVelocity.x = Mathf.Abs(m_simVelocity.y) * m_xVelocityCoefficient;

            Vector3 tmpVelocity = new Vector3(m_simVelocity.x, m_simVelocity.y, 0);
            rotation = Quaternion.Euler(0, 0, m_simulator.simuParas.moveDirAngle);
            tmpVelocity = rotation * tmpVelocity;

            velocity.x = tmpVelocity.x;
            velocity.y = tmpVelocity.y;

            position += velocity * deltaTime;

            float yVelocitySpreadCoefficient = m_simulator.simuParas.yVelocitySpreadCoefficient * Mathf.Abs(m_simVelocity.y);
            m_sizeSpread.y = Mathf.Max(m_sizeSpread.y, yVelocitySpreadCoefficient);

            m_sizeSpread *= Mathf.Pow(m_simulator.simuParas.sizeShrinkRate, deltaTime);
        }
        
        /// <summary>
        /// 更新痕迹，拖尾
        /// </summary>
        private void UpdateTrail()
        {
            if (Vector2.SqrMagnitude(position - m_lastTrailRaindropPos) <= m_nextTrailRaindropDistance * m_nextTrailRaindropDistance)
            {
                return;
            }
            if (Mass < 1000)
            {
                return;
            }

            // 创建痕迹雨滴
            float trailSize = Size.x * Random.Range(m_simulator.simuParas.trailSizeScaleRange[0], m_simulator.simuParas.trailSizeScaleRange[1]);
            //Vector2 trailPos = position + new Vector2(Random.Range(-5.0f, 5.0f), Size.y / 2);
            Vector2 deltaPos = new Vector2(Random.Range(-5.0f, 5.0f), Size.y / 2);
            Vector2 velocityDir = velocity.normalized;
            Vector2 trailPos = position - velocityDir * deltaPos.magnitude;
            Raindrop trailRaindrop = m_simulator.SpawnRaindrop(trailPos, trailSize, m_simulator.simuParas.trailRaindropDensity);
            if (trailRaindrop == null)
            {
                return;
            }

            //trailRaindrop.m_sizeSpread = new Vector2(0.1f, Mathf.Abs(velocity.y) * m_simulator.simuParas.trailRaindropSizeSpread);
            trailRaindrop.m_sizeSpread = new Vector2(0.1f, velocity.magnitude * m_simulator.simuParas.trailRaindropSizeSpread);
            trailRaindrop.parentRaindrop = this;

            Mass -= trailRaindrop.Mass;

            m_lastTrailRaindropPos = position;
            m_nextTrailRaindropDistance = Random.Range(m_simulator.simuParas.trailRaindropDistanceRange[0], m_simulator.simuParas.trailRaindropDistanceRange[1]);
        }
        
        /// <summary>
        /// 合并雨滴
        /// </summary>
        public void Merge(Raindrop target)
        {
            // 计算动量
            Vector2 selfMomentum = velocity * Mass;
            Vector2 targetMomentum = target.velocity * target.Mass;
            Vector2 totalMomentum = selfMomentum + targetMomentum;
            
            Mass += target.Mass;
            velocity = totalMomentum / Mass;

            selfMomentum = m_simVelocity * Mass;
            targetMomentum = target.m_simVelocity * target.Mass;
            totalMomentum = selfMomentum + targetMomentum;
            
            m_simVelocity = totalMomentum / Mass;
        }
        
    }
}