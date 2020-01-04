using Entities;
using Entities.Models;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repository.Repository
{
    public class FavoriteAdditionalMessageRepository : RepositoryBase<FavoriteAdditionalMessage>,
                                                       IFavoriteAdditionalMessageRepository
    {
        public FavoriteAdditionalMessageRepository(RepositoryContext repoContext) : base(repoContext)
        {

        }
    }
}
