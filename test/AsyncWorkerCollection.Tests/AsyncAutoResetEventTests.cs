using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class AsyncAutoResetEventTests
    {
        [ContractTestCase]
        public void WaitForSuccessOrResult()
        {
            "��ʹ�� Set �������� WaitOneAsync ����������� Set ֻ������һ��".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                // �ȼ���һ���ȴ����̣߳����ڵȴ���һ�ε� Set ��Ӧ�ĵȴ�
                var manualResetEvent = new ManualResetEvent(false);
                var task1 = Task.Run(async () =>
                {
                    var task = asyncAutoResetEvent.WaitOneAsync();
                    manualResetEvent.Set();
                    await task;
                    mock.Object.Do();
                });
                // ʹ�� manualResetEvent ���Եȴ��� task1 ִ�е��� WaitOne ����
                manualResetEvent.WaitOne();

                for (var i = 0; i < 5; i++)
                {
                    asyncAutoResetEvent.Set();
                }

                var taskList = new List<Task>();
                for (var i = 0; i < 5; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        Console.WriteLine("�������");
                        await asyncAutoResetEvent.WaitOneAsync();
                        mock.Object.Do();
                    });
                    taskList.Add(task);
                }

                foreach (var task in taskList)
                {
                    Task.WaitAny(task, Task.Delay(TimeSpan.FromSeconds(1)));
                }

                // Assert
                mock.Verify(job => job.Do(), Times.Exactly(2));
            });

            "�������� Set Ȼ���� WaitOneAsync ֻ��һ���߳�ִ��".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                asyncAutoResetEvent.Set();
                var task1 = Task.Run(async () =>
                {
                    await asyncAutoResetEvent.WaitOneAsync();
                    mock.Object.Do();
                });

                var task2 = Task.Run(async () =>
                {
                    await asyncAutoResetEvent.WaitOneAsync();
                    mock.Object.Do();
                });

                Task.WaitAny(task1, task2, Task.Delay(TimeSpan.FromSeconds(1)));

                // Assert
                mock.Verify(job => job.Do(), Times.Once);
            });

            "ʹ�� AsyncAutoResetEvent ����һ�� Set ��Ӧһ�� WaitOneAsync ���߳�ִ��".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                var taskList = new List<Task>(10);
                // ʹ�� SemaphoreSlim �ò����߳�ȫ������
                var semaphoreSlim = new SemaphoreSlim(0, 10);
                for (var i = 0; i < 10; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        var t = asyncAutoResetEvent.WaitOneAsync();
                        semaphoreSlim.Release();
                        await t;
                        mock.Object.Do();
                    });
                    taskList.Add(task);
                }

                // �ȴ� Task ������ await ����
                // ���û�еȴ������Զ����̴߳������棬��ʱ���ö�ε� Set ֻ������ʼ��
                // Ҳ���ǵ�ǰû���̵߳ȴ���Ȼ����ж�� Set ����
                for (int i = 0; i < 10; i++)
                {
                    semaphoreSlim.Wait();
                }
               
                for (var i = 0; i < 5; i++)
                {
                    asyncAutoResetEvent.Set();
                }

                foreach (var task in taskList)
                {
                    Task.WaitAny(task, Task.Delay(TimeSpan.FromSeconds(1)));
                }

                // Assert
                mock.Verify(job => job.Do(), Times.Exactly(5));
            });

            "���캯������Ϊ true �ȴ� WaitOneAsync ���̻߳�ִ��".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(true);
                var mock = new Mock<IFakeJob>();

                // Action
                var task = Task.Run(async () =>
                {
                    await asyncAutoResetEvent.WaitOneAsync();
                    mock.Object.Do();
                });

                Task.WaitAny(task, Task.Delay(TimeSpan.FromSeconds(1)));

                // Assert
                mock.Verify(job => job.Do(), Times.Once);
            });

            "���캯������Ϊ false �ȴ� WaitOneAsync ���̲߳���ִ��".Test(() =>
            {
                // Arrange
                var asyncAutoResetEvent = new AsyncAutoResetEvent(false);
                var mock = new Mock<IFakeJob>();

                // Action
                var task = Task.Run(async () =>
                {
                    await asyncAutoResetEvent.WaitOneAsync();
                    mock.Object.Do();
                });

                Task.WaitAny(task, Task.Delay(TimeSpan.FromSeconds(1)));

                // Assert
                mock.Verify(job => job.Do(), Times.Never);
            });

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

        public interface IFakeJob
        {
            void Do();
        }
    }
}