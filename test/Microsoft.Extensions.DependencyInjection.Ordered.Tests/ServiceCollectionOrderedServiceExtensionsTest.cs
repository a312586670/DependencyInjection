// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Ordered;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceCollectionOrderedServiceExtensionsTest
    {
        [Fact]
        public void AddOrdered_AllowsResolvingEmptyIOrdered()
        {
            // Arrange
            var collection = new ServiceCollection();
            collection.AddOrdered<IFakeService>();
            var provider = collection.BuildServiceProvider();

            // Act
            var ordered = provider.GetService<IOrdered<IFakeService>>();

            // Assert
            Assert.Empty(ordered);
        }

        [Fact]
        public void AddOrdered_AllowsResolvingAsIEnumerable()
        {
            // Arrange
            var collection = new ServiceCollection();
            collection.AddOrdered<IFakeService, FakeService>();
            var provider = collection.BuildServiceProvider();

            // Act
            var ordered = provider.GetService<IEnumerable<IFakeService>>();

            // Assert
            Assert.IsType<FakeService>(ordered.Single());
        }

        [Fact]
        public void AddOrdered_CachesInstances()
        {
            // Arrange
            var collection = new ServiceCollection();
            collection.AddOrdered<IFakeService, FakeService>();
            var provider = collection.BuildServiceProvider();

            // Act
            var ordered = provider.GetService<IOrdered<IFakeService>>();
            var array1 = ordered.ToArray();
            var array2 = ordered.ToArray();

            // Assert
            Assert.Equal((IEnumerable<IFakeService>)array1, array2);
        }

        [Fact]
        public void AddOrdered_SupportsTwoInstancesOfSameType()
        {
            // Arrange
            var collection = new ServiceCollection();
            collection.AddOrdered<IFakeService, FakeService>();
            collection.AddOrdered<IFakeService, FakeService>();
            var provider = collection.BuildServiceProvider();

            // Act
            var ordered = provider.GetService<IEnumerable<IFakeService>>();
            var array = ordered.ToArray();

            // Assert
            Assert.NotEqual(array[0], array[1]);
        }

        public static TheoryData AddOrderedOverloads
        {
            get
            {
                var serviceType = typeof(IFakeService);
                var implementationType = typeof(FakeServiceWithId);
                return new TheoryData<Action<IServiceCollection>>
                {
                    collection =>
                    {
                        collection.AddOrdered<IFakeService, FakeServiceWithId>();
                        collection.AddOrdered<IFakeService, FakeServiceWithId>(_ => new FakeServiceWithId(1));
                        collection.AddOrdered<IFakeService>(new FakeServiceWithId(2));
                    },
                    collection =>
                    {
                        collection.AddOrdered(serviceType, implementationType);
                        collection.AddOrdered(serviceType, _ => new FakeServiceWithId(1));
                        collection.AddOrdered(serviceType, new FakeServiceWithId(2));
                    },
                    collection =>
                    {
                        collection.AddOrdered((ServiceDescriptor) ServiceDescriptor.Singleton(serviceType, implementationType));
                        collection.AddOrdered((ServiceDescriptor) ServiceDescriptor.Singleton(serviceType, _ => new FakeServiceWithId(1)));
                        collection.AddOrdered((ServiceDescriptor) ServiceDescriptor.Singleton(serviceType, new FakeServiceWithId(2)));
                    },

                };
            }
        }

        [Theory]
        [MemberData(nameof(AddOrderedOverloads))]
        public void AddOrdered_SupportsAllServiceKinds(Action<IServiceCollection> addServices)
        {
            // Arrange
            var collection = new ServiceCollection();
            addServices(collection);
            var provider = collection.BuildServiceProvider();

            // Act
            var ordered = provider.GetService<IOrdered<IFakeService>>();
            var array = ordered.OfType<FakeServiceWithId>().ToArray();

            // Assert
            Assert.Equal(0, array[0].Id);
            Assert.Equal(1, array[1].Id);
            Assert.Equal(2, array[2].Id);
        }

        [Fact]
        public void RegistrationOrderIsPreservedWhenServicesAreIOrderedResolved()
        {
            // Arrange
            var collection = new ServiceCollection();
            collection.AddOrdered(typeof(IFakeMultipleService), typeof(FakeOneMultipleService));
            collection.AddOrdered(typeof(IFakeMultipleService), typeof(FakeTwoMultipleService));

            var provider = collection.BuildServiceProvider();

            collection = new ServiceCollection();
            collection.AddOrdered(typeof(IFakeMultipleService), typeof(FakeTwoMultipleService));
            collection.AddOrdered(typeof(IFakeMultipleService), typeof(FakeOneMultipleService));
            var providerReversed = collection.BuildServiceProvider();

            // Act
            var services = provider.GetService<IOrdered<IFakeMultipleService>>();
            var servicesReversed = providerReversed.GetService<IOrdered<IFakeMultipleService>>();

            // Assert
            Assert.Collection(services,
                service => Assert.IsType<FakeOneMultipleService>(service),
                service => Assert.IsType<FakeTwoMultipleService>(service));

            Assert.Collection(servicesReversed,
                service => Assert.IsType<FakeTwoMultipleService>(service),
                service => Assert.IsType<FakeOneMultipleService>(service));
        }
    }
}