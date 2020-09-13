using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class AsyncAutoResetEventTests
    {
        [ContractTestCase]
        public void Set()
        {
            "�� WaitOne ֮ǰ���ö�� Set ֻ���ڵ���֮����һ�� WaitOne ��������".Test(() =>
            {
                using var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                for (int i = 0; i < 1000; i++)
                {
                    asyncAutoResetEvent.Set();
                }

                var count = 0;
                var taskList = new List<Task>();
                for (int i = 0; i < 10; i++)
                {
                    taskList.Add(Task.Run(async () =>
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        await asyncAutoResetEvent.WaitOneAsync();
                        Interlocked.Increment(ref count);
                    }));
                }

                // ֻ��һ��ִ��
                // ��Ԫ������һ���ӣ�Ҳ�����ڲ�ͬ���豸�ϣ�Ҳ�����豸���ǲ������̣߳����������Ԫ����Ҳ�����ִ�е�ʱ�򣬷���û��һ���߳�ִ�����
                taskList.Add(Task.Delay(TimeSpan.FromSeconds(5)));
                // ���������һ���ȴ� 5 ����̣߳���ʱ��������һ���߳�ִ�����
                Task.WaitAny(taskList.ToArray());
                // ʲôʱ���� 0 ��ֵ����û�з����̣߳�Ҳ����û��һ�� Task.Run ����
                Assert.AreEqual(true, count <= 1);
                // һ���г��� 9 ���߳�û��ִ�����
                Assert.AreEqual(true, taskList.Count(task => !task.IsCompleted) >= 9);
            });
        }

        [ContractTestCase]
        public void ReleaseObject()
        {
            "�ڵ����ͷ�֮�����еĵȴ����ᱻ�ͷţ�ͬʱ�ͷŵ�ֵ�� false ֵ".Test(() =>
            {
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var manualResetEvent = new ManualResetEvent(false);
                var task = Task.Run(async () =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    var t = asyncAutoResetEvent.WaitOneAsync();
                    manualResetEvent.Set();

                    return await t;
                });
                // �����Ԫ�������� Task.Run ����̫��
                manualResetEvent.WaitOne();
                asyncAutoResetEvent.Dispose();

                task.Wait();
                var taskResult = task.Result;
                Assert.AreEqual(false, taskResult);
            });
        }
    }
}