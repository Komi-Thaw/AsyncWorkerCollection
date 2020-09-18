using System;
using System.Threading.Tasks;
using dotnetCampus.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MSTest.Extensions.Contracts;

namespace AsyncWorkerCollection.Tests
{
    [TestClass]
    public class DoubleBufferTaskTest
    {
        [ContractTestCase]
        public void DoAll()
        {
            "���̼߳�����������ִ���ٶȱȼ���죬���Եȴ���������ִ�����".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                var doubleBufferTask = new DoubleBufferTask<IFoo>(list =>
                {
                    foreach (var foo in list)
                    {
                        foo.Foo();
                    }

                    return Task.CompletedTask;
                });

                const int n = 10;

                var taskArray = new Task[100];

                for (int i = 0; i < taskArray.Length; i++)
                {
                    taskArray[i] = Task.Run(async () =>
                    {
                        for (int j = 0; j < n; j++)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(50));
                            doubleBufferTask.AddTask(mock.Object);
                        }
                    });
                }

                Task.WhenAll(taskArray).ContinueWith(_ => doubleBufferTask.Finish());

                doubleBufferTask.WaitAllTaskFinish().Wait();

                mock.Verify(foo => foo.Foo(), Times.Exactly(n * taskArray.Length));
            });

            "���̼߳������񣬿��Եȴ���������ִ�����".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                var doubleBufferTask = new DoubleBufferTask<IFoo>(async list =>
                {
                    foreach (var foo in list)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(10));
                        foo.Foo();
                    }
                });

                const int n = 10;

                var taskArray = new Task[10];

                for (int i = 0; i < taskArray.Length; i++)
                {
                    taskArray[i] = Task.Run(async () =>
                    {
                        for (int j = 0; j < n; j++)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(50));
                            doubleBufferTask.AddTask(mock.Object);
                        }
                    });
                }

                Task.WhenAll(taskArray).ContinueWith(_ => doubleBufferTask.Finish());

                doubleBufferTask.WaitAllTaskFinish().Wait();

                mock.Verify(foo => foo.Foo(), Times.Exactly(n * taskArray.Length));
            });

            "û�м������񣬵ȴ���ɣ����Եȴ����".Test(() =>
            {
                var mock = new Mock<IFoo>();
                mock.Setup(foo => foo.Foo());

                var doubleBufferTask = new DoubleBufferTask<IFoo>(async list =>
                {
                    foreach (var foo in list)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(50));
                        foo.Foo();
                    }
                });

                doubleBufferTask.Finish();

                doubleBufferTask.WaitAllTaskFinish().Wait();

                // û��ִ��һ��
                mock.Verify(foo => foo.Foo(), Times.Never);
            });
        }

        public interface IFoo
        {
            void Foo();
        }
    }
}