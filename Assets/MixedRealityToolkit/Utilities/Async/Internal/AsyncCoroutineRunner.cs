// MIT License

// Copyright(c) 2016 Modest Tree Media Inc

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities
{
    /// <summary>
    /// This Async Coroutine Runner is just a helper object to
    /// ensure that coroutines run properly with async/await.
    /// </summary>
    internal sealed class AsyncCoroutineRunner : MonoBehaviour
    {
        private static AsyncCoroutineRunner instance;

        private static readonly Queue<Action> Actions = new Queue<Action>();

        internal static AsyncCoroutineRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<AsyncCoroutineRunner>();
                }

                if (instance == null)
                {
                    var instanceGameObject = GameObject.Find("AsyncCoroutineRunner");

                    if (instanceGameObject != null)
                    {
                        instance = instanceGameObject.GetComponent<AsyncCoroutineRunner>();

                        if (instance == null)
                        {
                            Debug.Log("[AsyncCoroutineRunner] Found GameObject but didn't have component");

                            if (Application.isPlaying)
                            {
                                Destroy(instanceGameObject);
                            }
                            else
                            {
                                DestroyImmediate(instanceGameObject);
                            }
                        }
                    }
                }

                if (instance == null)
                {
                    instance = new GameObject("AsyncCoroutineRunner").AddComponent<AsyncCoroutineRunner>();
                }

                instance.gameObject.hideFlags = HideFlags.None;
#if !UNITY_EDITOR
                DontDestroyOnLoad(instance);
#endif

                return instance;
            }
        }

        internal static void Post(Action task)
        {
            lock (Actions)
            {
                Actions.Enqueue(task);
            }
        }

        private void Update()
        {
            Debug.Assert(Instance != null);

            int actionCount;

            lock (Actions)
            {
                actionCount = Actions.Count;
            }

            for (int i = 0; i < actionCount; i++)
            {
                Action next;

                lock (Actions)
                {
                    next = Actions.Dequeue();
                }

                next();
            }
        }
    }
}
