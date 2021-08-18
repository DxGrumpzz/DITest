namespace DITest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class Program
    {

        /// <summary>
        /// An enum that specifies Service lifetime
        /// </summary>
        public enum ScopeType
        {
            /// <summary>
            /// Service will live as long as the process is running
            /// </summary>
            Singleton = 0,

            /// <summary>
            /// Service will instantiated everytime it is requested
            /// </summary>
            Transient = 1,
        };


        /// <summary>
        /// A simple accessor for the DI
        /// </summary>
        private static class DI
        {
            /// <summary>
            /// A DI container which this class is "bound" to 
            /// </summary>
            private static DIContainer _container;

            /// <summary>
            /// Initialize DI 
            /// </summary>
            /// <param name="container"> The container to initialize from </param>
            public static void Initialize(DIContainer container)
            {
                _container = container;
            }

            /// <summary>
            /// Retrieve a service from the container
            /// </summary>
            /// <typeparam name="T"> The type of service </typeparam>
            /// <returns></returns>
            public static T GetService<T>()
            {
                return _container.GetService<T>();
            }
        };


        /// <summary>
        /// A service container
        /// </summary>
        public class DIContainer
        {

            /// <summary>
            /// A simple descriptor for a service
            /// </summary>
            public class ServiceDescriptor
            {
                /// <summary>
                /// The lifetime of this servie
                /// </summary>
                public ScopeType ScopeType { get; set; }

                /// <summary>
                /// The main Type of this service, this controls how this container finds the requested service
                /// </summary>
                public Type Type { get; set; }

                /// <summary>
                /// Describes the implementation if <see cref="Type"/> is an interface or other such type
                /// </summary>
                public Type ImplementationType { get; set; }

                /// <summary>
                /// The factory that will instantiate this service
                /// </summary>
                public Func<object> ImplementationFactory { get; set; }
            };


            /// <summary>
            /// The main list of bound services
            /// </summary>
            private Dictionary<Guid, ServiceDescriptor> _services = new Dictionary<Guid, ServiceDescriptor>();

            /// <summary>
            /// A list of singleton service that were instantiated
            /// </summary>
            private Dictionary<Guid, object> _singletonServices = new Dictionary<Guid, object>();


            /// <summary>
            /// Retrieve a bound service from the container
            /// </summary>
            /// <typeparam name="T"> The type of service </typeparam>
            /// <returns></returns>
            public T GetService<T>()
            {
                Guid typeID = GetTypeID<T>();

                // Find the requested service
                var serviceItem = _services[typeID];


                // If the requested service is a singleton...
                if (serviceItem.ScopeType == ScopeType.Singleton)
                {
                    // Find an instance from the singletons list
                    var instance = _singletonServices[typeID];

                    // If the instance is null...
                    if (instance == null)
                    {
                        // Get the factory function
                        var instanceFactory = serviceItem.ImplementationFactory;

                        if (instanceFactory != null)
                            // Create a new singleton instance from the factory function
                            instance = instanceFactory.Invoke();
                        else
                        {
                            // This might happen ? 
                            Debugger.Break();

                            // Constraining T to new() might not be a good idea since some "constructor-less" 
                            // types have to be called through GetService<T>() 
                            instance = Activator.CreateInstance(serviceItem.ImplementationType);
                        }

                        // Since the instance variable isn't actually *the* instance (reference), when we assign to it we need
                        // to also update the actual instance
                        _singletonServices[typeID] = instance;
                    };


                    return (T)instance;
                };


                // If the requested service is a transient...
                if (serviceItem.ScopeType == ScopeType.Transient)
                {
                    // Create an instance from the associated factory function
                    var instance = serviceItem.ImplementationFactory();

                    return (T)instance;
                };


                throw new Exception("How");
            }



            /// <summary>
            /// Add a Service as a singleton. 
            /// This method requires the Service to have a parameterless constructor
            /// </summary>
            /// <typeparam name="T"> The service type to add</typeparam>
            public void AddSingleton<T>()
                where T : class, new()
            {
                Guid typeID = GetTypeID<T>();

                // Create the service descriptor
                _services.Add(typeID, new ServiceDescriptor()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(T),
                    ScopeType = ScopeType.Singleton,

                    // Since this method requires the service to have a parameterless constructor,
                    // we can create a factory that returns an instance of T by calling said constructor
                    ImplementationFactory = () => { return new T(); },
                });


                // Add the service to the list of singletons that will be later invoked when requested
                _singletonServices.Add(typeID, null);
            }


            /// <summary>
            /// Add a service as a singleton, using a factory function to control how the service is created
            /// </summary>
            /// <typeparam name="T"> The service type </typeparam>
            /// <param name="implementationFactory"> The factory function </param>
            public void AddSingleton<T>(Func<T> implementationFactory)
                where T : class
            {
                Guid typeID = GetTypeID<T>();

                // Create service descriptor
                _services.Add(typeID, new ServiceDescriptor()
                {
                    Type = typeof(T),
                    ScopeType = ScopeType.Singleton,
                    ImplementationFactory = () => implementationFactory(),
                });

                // Add the service to the list of singletons that will be later invoked when requested
                _singletonServices.Add(typeID, null);
            }


            /// <summary>
            /// Add a service as a singleton, with an implementation.
            /// This function requires that <typeparamref name="TImplementation"/> inherit from <typeparamref name="T"/>,
            /// and that <typeparamref name="TImplementation"/> has a parameterless constructor
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <typeparam name="TImplementation"></typeparam>
            public void AddSingelton<T, TImplementation>()
                where T : class
                where TImplementation : T, new()
            {
                Guid typeID = GetTypeID<T>();

                // Create service descriptor
                _services.Add(typeID, new ServiceDescriptor()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(TImplementation),
                    ScopeType = ScopeType.Singleton,
                    ImplementationFactory = () => { return new TImplementation(); },
                });

                // Add the service to the list of singletons that will be later invoked when requested
                _singletonServices.Add(typeID, null);
            }


            /// <summary>
            /// Add a service as a singleton, with an implementation.
            /// This function requires that <typeparamref name="TImplementation"/> inherit from <typeparamref name="T"/>
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <typeparam name="TImplementation"></typeparam>
            /// <param name="implementationFactory"></param>
            public void AddSingleton<T, TImplementation>(Func<TImplementation> implementationFactory)
                where T : class
                where TImplementation : T
            {
                Guid typeID = GetTypeID<T>();

                // Create service descriptor
                _services.Add(typeID, new ServiceDescriptor()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(TImplementation),
                    ScopeType = ScopeType.Singleton,
                    ImplementationFactory = () => implementationFactory(),
                });

                // Add the service to the list of singletons that will be later invoked when requested
                _singletonServices.Add(typeID, null);
            }




            /// <summary>
            /// Add a service as a transient.
            /// This method requires <typeparamref name="T"/> to have a parameterless constructor
            /// </summary>
            /// <typeparam name="T"></typeparam>
            public void AddTransient<T>()
                where T : class, new()
            {
                Guid typeID = GetTypeID<T>();

                // Create service descriptor
                _services.Add(typeID, new ServiceDescriptor()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(T),
                    ScopeType = ScopeType.Transient,

                    ImplementationFactory = () => { return new T(); },
                });
            }


            /// <summary>
            /// Add a service as a transient, with a factory function to control how the service is created
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="implementationFactory"></param>
            public void AddTransient<T>(Func<T> implementationFactory)
                where T : class
            {
                Guid typeID = GetTypeID<T>();

                // Create service descriptor
                _services.Add(typeID, new ServiceDescriptor()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(T),
                    ScopeType = ScopeType.Transient,

                    ImplementationFactory = () => implementationFactory(),
                });
            }


            /// <summary>
            /// Add a service as a transient, with an implementation.
            /// This function requires that <typeparamref name="TImplementation"/> inherit from <typeparamref name="T"/>,
            /// and that <typeparamref name="TImplementation"/> has a parameterless constructor
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <typeparam name="TImplementation"></typeparam>
            public void AddTransient<T, TImplementation>()
                where T : class
                where TImplementation : T, new()
            {
                Guid typeID = GetTypeID<T>();

                // Create service descriptor
                _services.Add(typeID, new ServiceDescriptor()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(TImplementation),
                    ScopeType = ScopeType.Transient,

                    ImplementationFactory = () => { return new TImplementation(); },
                });

            }


            /// <summary>
            /// Add a service as a transient, with an implementation.
            /// This function requires that <typeparamref name="TImplementation"/> inherit from <typeparamref name="T"/>
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <typeparam name="TImplementation"></typeparam>
            /// <param name="implementationFactory"></param>
            public void AddTransient<T, TImplementation>(Func<TImplementation> implementationFactory)
                where T : class
                where TImplementation : T
            {
                Guid typeID = GetTypeID<T>();

                // Create service descriptor
                _services.Add(typeID, new ServiceDescriptor()
                {
                    Type = typeof(T),
                    ImplementationType = typeof(TImplementation),
                    ScopeType = ScopeType.Transient,

                    ImplementationFactory = () => implementationFactory(),
                });
            }


            /// <summary>
            /// Get a unique ID from a Type
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            private Guid GetTypeID<T>()
            {
                // Thank you microsoft for making my life easier
                return typeof(T).GUID;
            }

        };



        public interface ITestClassSingelton
        {
            public string GUID { get; set; }
        }

        public class TestClassSingelton : ITestClassSingelton
        {
            private TestClass2Singelton _testClass2;
            public string GUID { get; set; } = Guid.NewGuid().ToString("N");


            public TestClassSingelton()
            {
                _testClass2 = DI.GetService<TestClass2Singelton>();
            }

        }
        public class TestClass2Singelton
        {
            private TestClassSingelton _testClass;

            public string GUID { get; } = Guid.NewGuid().ToString("N");


            public TestClass2Singelton()
            {
                // _testClass = DI.GetService<TestClass>();
            }
        };

        public class TestClass3SingeltonCtor
        {
            private TestClass2Singelton _testClass2;
            public string GUID { get; set; } = Guid.NewGuid().ToString("N");


            public TestClass3SingeltonCtor(string guid)
            {
                GUID = guid;
                _testClass2 = DI.GetService<TestClass2Singelton>();
            }

        }


        public interface ITestClassTransient
        {
            public string GUID { get; set; }
        }

        public class TestClassTransient : ITestClassTransient
        {
            private TestClass2Singelton _testClass2;
            public string GUID { get; set; } = Guid.NewGuid().ToString("N");


            public TestClassTransient()
            {
                _testClass2 = DI.GetService<TestClass2Singelton>();
            }

        }

        public interface ITestClassTransient2
        {
            public string GUID { get; }
        }

        public class TestClass2Transient : ITestClassTransient2
        {
            private TestClassSingelton _testClass;

            public string GUID { get; } = Guid.NewGuid().ToString("N");

            public TestClass2Transient(string guid)
            {
                GUID = guid;
            }
        }


        public static void Main(string[] args)
        {
            DIContainer di = new DIContainer();

            di.AddSingleton<TestClass2Singelton>();
            di.AddSingleton<TestClassSingelton>(() =>
            {
                return new TestClassSingelton()
                {
                    GUID = "2",
                };
            });
            di.AddSingleton<ITestClassSingelton, TestClassSingelton>(() =>
            {
                return new TestClassSingelton()
                {
                    GUID = "1",
                };
            });
            di.AddSingleton<TestClass3SingeltonCtor>(() =>
            {
                return new TestClass3SingeltonCtor("3");
            });


            di.AddTransient<TestClassTransient>();
            di.AddTransient<TestClass2Transient>(() =>
            {
                return new TestClass2Transient("2");
            });
            di.AddTransient<ITestClassTransient, TestClassTransient>(() =>
            {
                return new TestClassTransient();
            });
            di.AddTransient<ITestClassTransient2, TestClass2Transient>(() =>
            {
                return new TestClass2Transient("3");
            });

            DI.Initialize(di);


            // Singeltons
            {

                // Reference validation
                {

                    var testClass = DI.GetService<ITestClassSingelton>();
                    var testClass2 = DI.GetService<ITestClassSingelton>();
                    var testClass4 = DI.GetService<ITestClassSingelton>();

                    if (!((testClass == testClass2) &&
                        (testClass2 == testClass4)))
                    {
                        Debugger.Break();
                    }
                };

                {

                    var testClass = DI.GetService<TestClass2Singelton>();
                    var testClass2 = DI.GetService<TestClass2Singelton>();
                    var testClass4 = DI.GetService<TestClass2Singelton>();

                    if (!((testClass == testClass2) &&
                        (testClass2 == testClass4)))
                    {
                        Debugger.Break();
                    }
                };


                // Factory-initialization validation
                {
                    var testClass = DI.GetService<ITestClassSingelton>();

                    if (testClass.GUID != "1")
                    {
                        Debugger.Break();
                    }
                };

                {
                    var instance1 = DI.GetService<TestClassSingelton>();
                    var instace2 = DI.GetService<TestClassSingelton>();

                    if (instance1 != instace2)
                        Debugger.Break();

                }


                // Reference validation 2, w/ implementation factory
                {
                    var instance1 = DI.GetService<TestClassSingelton>();
                    var instace2 = DI.GetService<TestClassSingelton>();

                    if (instance1.GUID != instace2.GUID)
                        Debugger.Break();
                };

                {
                    var instance1 = DI.GetService<TestClassSingelton>();
                    var instace2 = DI.GetService<TestClassSingelton>();

                    if ((instance1.GUID != "2") ||
                        (instace2.GUID != "2"))
                        Debugger.Break();
                };


                // Reference validation 3, Non parameterless constructor w/ implementation factory 
                {
                    var instance = DI.GetService<TestClass3SingeltonCtor>();
                    var instance2 = DI.GetService<TestClass3SingeltonCtor>();

                    if (instance != instance2)
                        Debugger.Break();
                };

                {
                    var instance = DI.GetService<TestClass3SingeltonCtor>();
                    var instance2 = DI.GetService<TestClass3SingeltonCtor>();

                    if ((instance.GUID != "3") ||
                        (instance2.GUID != "3"))
                        Debugger.Break();
                };

            }


            // Transients
            {

                // Reference validation
                {

                    var instance = DI.GetService<TestClassTransient>();
                    var instance2 = DI.GetService<TestClassTransient>();

                    if (instance == instance2)
                        Debugger.Break();
                };

                {
                    var instance = DI.GetService<TestClassTransient>();
                    var instance2 = DI.GetService<TestClassTransient>();

                    if (instance.GUID == instance2.GUID)
                        Debugger.Break();
                };


                // Reference validation w/ interfaces
                {
                    var instance = DI.GetService<ITestClassTransient>();
                    var instance2 = DI.GetService<ITestClassTransient>();

                    if (instance == instance2)
                        Debugger.Break();
                };

                {
                    var instance = DI.GetService<ITestClassTransient>();
                    var instance2 = DI.GetService<ITestClassTransient>();

                    if (instance.GUID == instance2.GUID)
                        Debugger.Break();
                };

                // Reference validation, interfaces w/ ctor
                {
                    var instance = DI.GetService<ITestClassTransient2>();
                    var instance2 = DI.GetService<ITestClassTransient2>();

                    if (instance == instance2)
                        Debugger.Break();
                };

                {
                    var instance = DI.GetService<ITestClassTransient2>();
                    var instance2 = DI.GetService<ITestClassTransient2>();

                    if ((instance.GUID != "3") &&
                        (instance2.GUID != "3"))
                        Debugger.Break();
                };


                {
                    var instance = DI.GetService<TestClass2Transient>();
                    var instance2 = DI.GetService<TestClass2Transient>();

                    if (instance == instance2)
                        Debugger.Break();
                };

                {
                    var instance = DI.GetService<TestClass2Transient>();
                    var instance2 = DI.GetService<TestClass2Transient>();

                    if ((instance.GUID != "2") &&
                        (instance2.GUID != "2"))
                        Debugger.Break();
                };

            };

            Console.WriteLine("Hello World!");
        }
    }
}