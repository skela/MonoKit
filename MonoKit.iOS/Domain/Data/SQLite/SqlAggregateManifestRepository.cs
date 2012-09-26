//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="SqlAggregateManifestRepository.cs" company="sgmunn">
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

namespace MonoKit.Domain.Data.SQLite
{
    using System;
    using MonoKit.Data.SQLite;
    using MonoKit.Data;
    using MonoKit.Tasks;

    public class SqlAggregateManifestRepository : IAggregateManifestRepository
    {
        private const string UpdateSql = "update AggregateManifest set Version = ? where Id = ? and Version = ?";

        private readonly SQLiteConnection connection;

        public SqlAggregateManifestRepository(SQLiteConnection connection)
        {
            this.connection = connection;
        }

        public void UpdateManifest(IUniqueIdentity id, int currentVersion, int newVersion)
        {
            Console.WriteLine(string.Format("Update manifest {0} - {1} - {2}", id, currentVersion, newVersion));

            try
            {
            var updated = SynchronousTask.GetSync(() => this.DoUpdate(id, currentVersion, newVersion));

            if (!updated)
            {
                Console.WriteLine("AggregateManifest FAILED");
                throw new ConcurrencyException();
            }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("** Unable to update Aggregate Manifest **", ex);
            }
        }

        private bool DoUpdate(IUniqueIdentity id, int currentVersion, int newVersion)
        {
            if (currentVersion == 0)
            {
                Console.WriteLine("..insert");
                this.connection.Insert(new AggregateManifest { Id = id.Id, Version = newVersion, AggregateType = id.GetType().Name });
            }
            else
            {
                Console.WriteLine("..update");
                var rows = this.connection.Execute(UpdateSql, newVersion, id.Id, currentVersion);
                return rows == 1;
            }

            return true;
        }
    }
}
