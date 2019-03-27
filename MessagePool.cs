using System;
using System.Collections.Generic;
using System.Linq;
using Holoville.HOTween;
using uGUI.Carousel;
using UnityEngine;
using Object = UnityEngine.Object;

public class MessagePool : IDisposable
{
    protected Dictionary<Type, Stack<IReinitable>> pool;
    protected Transform parent;
    protected int maxStackCount;

    public MessagePool(int _maxStackCount = 6)
    {
        maxStackCount = _maxStackCount;
        pool = new Dictionary<Type, Stack<IReinitable>>();
        parent = new GameObject("_PoolParent").transform;
    }

    public IReinitable Get(Type type)
    {
        if (!pool.ContainsKey(type) || !pool[type].Any())
            return null;

        var result = pool[type].Pop();
        return result;
    }

    public void Put(IReinitable vm, Type type)
    {
        var transform = (vm as MonoBehaviour).transform;
        HOTween.Kill(transform);
        (transform as RectTransform).localScale = Vector3.one;
        transform.SetParent(parent);
        
        if (!pool.ContainsKey(type))
        {
            pool.Add(type, new Stack<IReinitable>());           
        }

        if (pool[type].Count >= maxStackCount)
        {
            Object.Destroy(transform.gameObject);
            return;
        }

        pool[type].Push(vm);
    }

    public void Dispose()
    {
        pool.Clear();
        if (parent != null)
        {
            Object.Destroy(parent.gameObject);
        }
    }
};
