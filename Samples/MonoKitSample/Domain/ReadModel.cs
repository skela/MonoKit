//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ReadModel.cs" company="sgmunn">
//    (c) sgmunn 2012  
//
//    Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//    documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//    the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and 
//    to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//    The above copyright notice and this permission notice shall be included in all copies or substantial portions of 
//    the Software.
//
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
//    THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//    CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
//    IN THE SOFTWARE.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------
//

namespace MonoKitSample.Domain
{
    using System;
    using MonoKit.Data;
    using MonoKit.Data.SQLite;
    using MonoKit.Domain.Data;

    public class TestReadModel : IDataModel
    {
        public IUniqueIdentity Identity
        {
            get
            {
                return new TestReadModelId(this.Id);
            }
        }

        [PrimaryKey]
        public Guid Id { get; set;}

        public class TestReadModelId : Identity
        {
            public TestReadModelId(Guid id) : base(id)
            {
            }
        }
    }
    
    public class TestBuilder : ReadModelBuilder
    {
        public TestBuilder(IRepository<TestReadModel> repository)
        {
        }
        
        public void HandleMe(TestEvent1 @event)
        {
            Console.WriteLine("Builder TestEvent1 {0}", @event.Identity);
        }
        
        public void HandleMe(TestEvent2 @event)
        {
            Console.WriteLine("Builder TestEvent2 {0}", @event.Identity);
        }
    }

    public class TransactionDataContract : IDataModel
    {
        public TransactionDataContract()
        {
            this.Id = Guid.NewGuid();
        }

        [Ignore]
        public IUniqueIdentity Identity
        {
            get
            {
                return new TransactionId(this.Id);
            }

        }

        [PrimaryKey]
        public Guid Id { get; set; }

        [Indexed]
        public Guid TestId { get; set; }
        
        public string Description { get; set; }
        
        public decimal Amount { get; set; }

        public class TransactionId : Identity
        {
            public TransactionId(Guid id) : base(id)
            {
            }
        }
    }
    
    public class TransactionReadModelBuilder : ReadModelBuilder
    {
        private IRepository<TransactionDataContract> repository;

        public TransactionReadModelBuilder(IRepository<TransactionDataContract> repository)
        {
            Console.WriteLine("read model created");
            this.repository = repository;
        }
        
        public void Handle(TestEvent2 evt)
        {
            Console.WriteLine("read model updated");
            var transaction = this.repository.New();

            transaction.TestId = evt.Identity.Id;
            transaction.Amount = evt.Amount;
            transaction.Description = evt.Description;

            this.repository.Save(transaction);
        }
    }
}
