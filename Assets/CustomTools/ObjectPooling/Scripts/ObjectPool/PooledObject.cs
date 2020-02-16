using UnityEngine;

public class PooledObject : MonoBehaviour, IPooledObject
{

    public PooledObjectType Type;

    private void Awake()
    {

    }

    public virtual void OnObjectSpawn()
    {

    }

    public virtual void OnObjectDespawn()
    {

    }

    public virtual void Init()
    {

    }

    public void Despawn()
    {
        ObjectPooler.Instance.Despawn(Type, gameObject);
    }
}
