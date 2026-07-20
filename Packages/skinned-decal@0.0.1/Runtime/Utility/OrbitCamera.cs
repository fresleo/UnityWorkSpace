using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SkinnedDecals
{
    /// <summary>
    /// 摄像机的轨道控制
    /// </summary>
    public class OrbitCamera : MonoBehaviour
    {
        public Transform target;
        public float speed = 500f, zoomSpeed = 100f, offsetSpeed = 5f;

        private Vector3 m_offset = Vector3.zero;

        private void Start()
        {
            Orbit(0f, 0f, 0f);
        }

        private void Update()
        {
            float zoom = Input.GetAxis("Mouse ScrollWheel");
            if (Input.GetMouseButton(1) || zoom != 0f)
            {
                float horizontal = Input.GetAxis("Mouse X");
                float vertical = Input.GetAxis("Mouse Y");
                Orbit(horizontal, vertical, zoom);
            }

            if (Input.GetMouseButton(2))
            {
                float horizontal = Input.GetAxis("Mouse X");
                float vertical = Input.GetAxis("Mouse Y");
                Offset(horizontal, vertical);
            }
        }

        private void Orbit(float horizontal, float vertical, float zoom)
        {
            Vector3 targetPos = target.position + m_offset;

            float distance = Vector3.Distance(transform.position, targetPos) + -zoom * Time.deltaTime * zoomSpeed;

            Vector3 direction = (transform.position - targetPos).normalized;
            direction = Quaternion.AngleAxis(horizontal * Time.deltaTime * speed, transform.up) * direction;
            transform.position = targetPos + direction * distance;

            direction = (transform.position - targetPos).normalized;
            direction = Quaternion.AngleAxis(vertical * Time.deltaTime * speed, -transform.right) * direction;
            transform.position = targetPos + direction * distance;

            transform.LookAt(targetPos);
        }

        private void Offset(float horizontal, float vertical)
        {
            Vector3 targetPos = target.position + m_offset;
            Vector3 direction = (transform.position - targetPos).normalized;
            float distance = Vector3.Distance(transform.position, targetPos);

            m_offset += horizontal * -transform.right * Time.deltaTime * offsetSpeed + vertical * -transform.up * Time.deltaTime * offsetSpeed;

            targetPos = target.position + m_offset;
            transform.position = targetPos + direction * distance;
            transform.LookAt(targetPos);
        }
    }
}