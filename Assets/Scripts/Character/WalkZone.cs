using UnityEngine;

public class WalkZone : MonoBehaviour
{
    [SerializeField] private Vector3 size = new Vector3(3f, 0f, 3f);

    public Vector3 GetRandomPoint()
    {
        Vector3 center = transform.position;
        float x = center.x + Random.Range(-size.x * 0.5f, size.x * 0.5f);
        float z = center.z + Random.Range(-size.z * 0.5f, size.z * 0.5f);
        return new Vector3(x, center.y, z);
    }

    public bool Contains(Vector3 worldPoint)
    {
        Vector3 local = worldPoint - transform.position;
        return Mathf.Abs(local.x) <= size.x * 0.5f &&
               Mathf.Abs(local.z) <= size.z * 0.5f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
        Gizmos.DrawCube(transform.position + Vector3.up * 0.01f, new Vector3(size.x, 0.02f, size.z));
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.9f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.01f, new Vector3(size.x, 0.02f, size.z));
    }
#endif
}
