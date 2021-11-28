using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pool<T> : List<T>
{
    private int _CurrentIndex = 0;

    public T NextRandom()
    {
        if (Empty())
        {
            Reset();
        }
        return this[_CurrentIndex++];
    }

    public bool Empty()
    {
        return _CurrentIndex >= Count;
    }

    public void Reset() 
    {
        _CurrentIndex = 0;
        Shuffle();
    }

    private void Shuffle()
    {
        System.Random random = new System.Random();
        int n = Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            T value = this[k];
            this[k] = this[n];
            this[n] = value;
        }
    }
}
