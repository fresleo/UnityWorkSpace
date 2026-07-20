// Created By: WangYu  Date: 2024-11-18

using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RaindropEffect
{
    /// <summary>
    /// 模拟参数
    /// </summary>
    [Serializable]
    public class SimulateParameters
    {
        /// <summary>
        /// 屏幕尺寸
        /// </summary>
        public int screenWidth = 1920, screenHeight = 1080;
        
        /// <summary>
        /// 生成间隔
        /// </summary>
        public Vector2 spawnInterval = new(0.05f, 0.2f);
        /// <summary>
        /// 雨滴的尺寸范围
        /// </summary>
        public Vector2 raindropSizeRange = new(50f, 100f);
        /// <summary>
        /// 雨滴的最大数量
        /// </summary>
        [Range(100, 1023)] public int raindropMaxCount = 1023;
        /// <summary>
        /// 初始化尺寸散布
        /// </summary>
        [Range(0, 1)] public float initSizeSpread = 0.5f;
        
        /// <summary>
        /// 重力
        /// </summary>
        public float gravity = 2200;
        /// <summary>
        /// 摩擦力系数
        /// </summary>
        public float frictionForceCoefficient = 4;
        
        /// <summary>
        /// 强制更新间隔范围
        /// </summary>
        public Vector2 forceUpdateIntervalRange = new(0.1f, 0.5f);
        /// <summary>
        /// x轴速度系数范围
        /// </summary>
        public Vector2 xVelocityCoefficientRange = new(0, 0.3f);
        /// <summary>
        /// 质量蒸发速率 (每秒)
        /// </summary>
        [Range(0, 60)] public float massEvaporateRate = 15;
        /// <summary>
        /// y轴速度扩散系数
        /// </summary>
        public float yVelocitySpreadCoefficient = 0.0001f;
        /// <summary>
        /// 移动方向的角度
        /// </summary>
        [Range(-180, 180)] public float moveDirAngle = 0;
        /// <summary>
        /// 雨滴收缩速率的大小（每秒）
        /// </summary>
        public float sizeShrinkRate = 0.01f;

        /// <summary>
        /// 拖尾尺寸范围
        /// </summary>
        public Vector2 trailSizeScaleRange = new(0.3f, 0.5f);
        /// <summary>
        /// 拖尾雨滴的密度
        /// </summary>
        public float trailRaindropDensity = 0.2f;
        /// <summary>
        /// 拖尾雨滴的尺寸扩散
        /// </summary>
        [Range(0, 0.1f)] public float trailRaindropSizeSpread = 0.006f;
        /// <summary>
        /// 拖尾雨滴的距离范围
        /// </summary>
        public Vector2 trailRaindropDistanceRange = new(15, 35);
    }

    /// <summary>
    /// 分区网格
    /// </summary>
    public class Grid
    {
        /// <summary>
        /// 归属于网格的雨滴
        /// </summary>
        public List<Raindrop> raindropList = new();

        public int GetRaindropCount()
        {
            return raindropList.Count;
        }

        public void Add(Raindrop raindrop)
        {
            raindropList.Add(raindrop);
            raindrop.belongToGrid = this;
        }

        public void Remove(Raindrop raindrop)
        {
            raindropList.Remove(raindrop);
            raindrop.belongToGrid = null;
        }
    }
    
    /// <summary>
    /// 雨滴模拟器
    /// </summary>
    public class RaindropSimulator
    {
        /// <summary>
        /// 模拟参数
        /// </summary>
        public SimulateParameters simuParas;
        
        /// <summary>
        /// 所有的雨滴
        /// </summary>
        public List<Raindrop> raindropList = new();
        
        /// <summary>
        /// 启用生成
        /// </summary>
        public bool enableSpawning;
        
        // 网格
        private List<Grid> m_gridList = new();
        // 网格的行列
        private int m_gridColumnCount, m_gridRowCount;
        
        private Rect m_spawnRect;
        private float m_currentSpawnTime, m_nextSpawnTime;
        
        
        /// <summary>
        /// 清理
        /// </summary>
        public void Clear()
        {
            this.enableSpawning = false;
            
            m_currentSpawnTime = 0;
            m_nextSpawnTime = 0;
            
            foreach (var grid in m_gridList)
            {
                grid.raindropList.Clear();
            }
            m_gridList.Clear();
            
            raindropList.Clear();
        }

        /// <summary>
        /// 重置尺寸
        /// </summary>
        public void Resize()
        {
            m_spawnRect = new Rect(0, 0, simuParas.screenWidth, simuParas.screenHeight);
            
            // 创建格子
            float gridSize = GetGridSize();
            m_gridColumnCount = (int)Mathf.Ceil(simuParas.screenWidth / gridSize);
            m_gridRowCount = (int)Mathf.Ceil(simuParas.screenHeight / gridSize);

            m_gridList.Clear();
            int totalCount = m_gridColumnCount * m_gridRowCount;
            for (int i = 0; i < totalCount; i++)
            {
                m_gridList.Add(new Grid());
            }
        }
        
        /// <summary>
        /// 获取格子尺寸
        /// </summary>
        public float GetGridSize()
        {
            return simuParas.raindropSizeRange[1] * 0.5f;
        }

        /// <summary>
        /// 更新
        /// </summary>
        public void Update(float deltaTime)
        {
            if (simuParas == null) return;

            // 生成雨滴
            if (enableSpawning)
            {
                SpawnRaindrops(deltaTime);
            }
            // 更新雨滴
            UpdateRaindrops(deltaTime);
            
            // 检查碰撞
            CheckCollision();
            
            // 回收雨滴
            for (int i = raindropList.Count - 1; i >= 0; i--)
            {
                var raindrop = raindropList[i];
                if(!raindrop.willBeDestroyed) continue;
                
                // 从网格中移除
                if (raindrop.belongToGrid != null)
                {
                    raindrop.belongToGrid.Remove(raindrop);
                }
                
                // 从列表中移除
                raindropList.RemoveAt(i);
            }
        }
        
        private void SpawnRaindrops(float deltaTime)
        {
            // 超出限制了
            if (raindropList.Count > simuParas.raindropMaxCount) return;

            // 检查生成间隔
            m_currentSpawnTime += deltaTime;
            if(m_currentSpawnTime <= m_nextSpawnTime) return;
            
            Random.InitState((int)DateTime.Now.Ticks);
            Vector2 pos = RandomInRect(m_spawnRect);
            float size = Random.Range(simuParas.raindropSizeRange[0], simuParas.raindropSizeRange[1]);
            
            Raindrop newRaindrop = new Raindrop(this, pos, size);
            raindropList.Add(newRaindrop);
            
            m_nextSpawnTime += Random.Range(simuParas.spawnInterval[0], simuParas.spawnInterval[1]);
        }
        
        /// <summary>
        /// 在矩形中随机1个点
        /// </summary>
        public static Vector2 RandomInRect(Rect rect)
        {
            float randomWidth = Random.Range(0.0f, 1.0f) * rect.size.x;
            float randomHeight = Random.Range(0.0f, 1.0f) * rect.size.y;
            var pos = new Vector2(randomWidth, randomHeight) + rect.min;
            return pos;
        }
        
        /// <summary>
        /// 产生一个雨滴
        /// </summary>
        public Raindrop SpawnRaindrop(Vector2 pos, float size, float density = 1)
        {
            if (raindropList.Count > simuParas.raindropMaxCount) return null;

            Raindrop newRaindrop = new Raindrop(this, pos, size, density);
            raindropList.Add(newRaindrop);

            Grid grid = GetGridAtWorldPos(newRaindrop.position.x, newRaindrop.position.y);
            grid?.Add(newRaindrop);

            return newRaindrop;
        }
        
        
        #region 索引格子的方法
        
        private Grid GetGridAtWorldPos(float x, float y)
        {
            Vector2 gridXY = GetGridIndexByWorldPos(x, y);
            Grid grid = GetGridAt((int)gridXY.x, (int)gridXY.y);
            return grid;
        }
        
        private Vector2 GetGridIndexByWorldPos(float x, float y)
        {
            float gridSize = GetGridSize();
            int gridX = (int)Mathf.Floor(x / gridSize);
            int gridY = (int)Mathf.Floor(y / gridSize);
            var gridIndex = new Vector2(gridX, gridY);
            return gridIndex;
        }
        
        private Grid GetGridAt(int gridIndexX, int gridIndexY)
        {
            if (gridIndexX < 0 || gridIndexY < 0)
            {
                return null;
            }

            int index = gridIndexY * m_gridColumnCount + gridIndexX;
            if (index >= m_gridList.Count)
            {
                return null;
            }

            return m_gridList[index];
        }
        
        #endregion // 索引格子的方法
        
        
        /// <summary>
        /// 更新雨滴
        /// </summary>
        private void UpdateRaindrops(float deltaTime)
        {
            int raindropListCount = raindropList.Count;
            int gridListCount = m_gridList.Count;
            
            for (int i = 0; i < raindropListCount; i++)
            {
                Raindrop raindrop = raindropList[i];
                if (raindrop.willBeDestroyed) continue;

                raindrop.Update(deltaTime);

                // 出屏了，准备回收
                float raindropMaxSize = simuParas.raindropSizeRange[1];
                //if (raindrop.position.y < -raindropMaxSize)
                if (raindrop.position.y < m_spawnRect.yMin - raindropMaxSize 
                    || raindrop.position.y > m_spawnRect.yMax + raindropMaxSize 
                    || raindrop.position.x < m_spawnRect.xMin - raindropMaxSize 
                    || raindrop.position.x > m_spawnRect.xMax + raindropMaxSize)
                {
                    raindrop.willBeDestroyed = true;
                }

                if (raindrop.willBeDestroyed) continue;

                // 雨滴切换所属的格子
                Vector2 gridIndex = GetGridIndexByWorldPos(raindrop.position.x, raindrop.position.y);
                int gridIndexX = (int)gridIndex.x;
                int gridIndexY = (int)gridIndex.y;
                if (gridIndexX < 0 || gridIndexY < 0) continue;

                int index = gridIndexY * m_gridColumnCount + gridIndexX;
                if (index >= gridListCount) continue;

                Grid newGrid = m_gridList[index];
                if (newGrid != raindrop.belongToGrid)
                {
                    raindrop.belongToGrid?.Remove(raindrop);

                    newGrid?.Add(raindrop);
                    raindrop.belongToGrid = newGrid;
                }
            }
        }

        /// <summary>
        /// 检查碰撞
        /// </summary>
        private void CheckCollision()
        {
            int raindropListCount = raindropList.Count;
            int gridListCount = m_gridList.Count;
            
            for (int i = 0; i < raindropListCount; i++)
            {
                Raindrop raindrop = raindropList[i];
                if (raindrop.willBeDestroyed) continue;

                Vector2 gridIndex = GetGridIndexByWorldPos(raindrop.position.x, raindrop.position.y);
                int gridIndexX = (int)gridIndex.x;
                int gridIndexY = (int)gridIndex.y;

                // 雨滴只与九宫格内的雨滴进行碰撞检测，减少计算量
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        int neighbourGridIndexX = gridIndexX + offsetX;
                        int neighbourGridIndexY = gridIndexY + offsetY;
                        if (neighbourGridIndexX < 0 || neighbourGridIndexY < 0) continue;

                        int neighbourGridIndex = neighbourGridIndexY * m_gridColumnCount + neighbourGridIndexX;
                        if (neighbourGridIndex >= gridListCount) continue;

                        Grid grid = m_gridList[neighbourGridIndex];
                        if (grid == null) continue;

                        for (int n = 0; n < grid.raindropList.Count; ++n)
                        {
                            Raindrop other = grid.raindropList[n];
                            if(other.willBeDestroyed) continue;
                            
                            // 是同1个
                            bool isSame = (other == raindrop);
                            // 1个是另1个的父
                            bool isParent = (other.parentRaindrop == raindrop) || (raindrop.parentRaindrop == other);
                            // 是1个父
                            bool isSameParent = (raindrop.parentRaindrop != null) && (raindrop.parentRaindrop == other.parentRaindrop);
                            if (isSame || isParent || isSameParent)
                            {
                                continue;
                            }
                            
                            // 合并雨滴
                            float distance = Vector2.Distance(raindrop.position, other.position);
                            if (distance < raindrop.GetMergeDistance() + other.GetMergeDistance())
                            {
                                if (raindrop.Mass >= other.Mass)
                                {
                                    raindrop.Merge(other);
                                    other.willBeDestroyed = true;
                                }
                                else
                                {
                                    other.Merge(raindrop);
                                    raindrop.willBeDestroyed = true;
                                }
                            }
                        }
                    }
                }
            }
        }
        
    }
}