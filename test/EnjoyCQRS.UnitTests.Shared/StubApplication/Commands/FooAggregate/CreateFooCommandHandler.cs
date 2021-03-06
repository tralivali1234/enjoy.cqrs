﻿using System.Threading.Tasks;
using EnjoyCQRS.Commands;
using EnjoyCQRS.EventSource.Storage;
using EnjoyCQRS.UnitTests.Shared.StubApplication.Domain.FooAggregate;

namespace EnjoyCQRS.UnitTests.Shared.StubApplication.Commands.FooAggregate
{
    public class CreateFooCommandHandler : ICommandHandler<CreateFooCommand>
    {
        private readonly IRepository _repository;

        public CreateFooCommandHandler(IRepository repository)
        {
            _repository = repository;
        }

        public async Task ExecuteAsync(CreateFooCommand command)
        {
            var foo = new Foo(command.AggregateId);
            await _repository.AddAsync(foo);
        }
    }
}