﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Glass.Mapper.Sc.Configuration;
using Glass.Mapper.Sc.ContentSearch.Pipelines.ObjectConstruction.Tasks.SearchProxy;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.Data;

namespace Glass.Mapper.Sc.ContentSearch.LuceneProvider
{
    public class GlassDocumentMapperObjectFactory : IIndexDocumentPropertyMapperObjectFactory, ISearchIndexInitializable
    {
        private ISearchIndex _searchIndex;
        private readonly DefaultDocumentMapperObjectFactory _defaultDocumentMapper = new DefaultDocumentMapperObjectFactory();

        public List<string> GetTypeIdentifyingFields(Type baseType, IEnumerable<IExecutionContext> executionContexts)
        {
			var typeConfig = Context.Contexts.Any() ?
				Context.Contexts.Select(c => c.Value).Select(c => c.GetTypeConfigurationFromType<SitecoreTypeConfiguration>(baseType)).FirstOrDefault(c => c!=null)
				: null;

            if (typeConfig == null || typeConfig.TemplateId == (ID)null)
                return _defaultDocumentMapper.GetTypeIdentifyingFields(baseType, executionContexts);

            var result = typeConfig.Properties.Select(p => _searchIndex.FieldNameTranslator.GetIndexFieldName((MemberInfo) p.PropertyInfo));
            return result.ToList();
        }

        public List<Type> GetPotentialCreatedTypes(Type baseType, IEnumerable<IExecutionContext> executionContexts)
        {
            var typeConfig = Context.Contexts.Any() ?
				Context.Contexts.SelectMany(c => c.Value.TypeConfigurations).Where(tc => baseType.IsAssignableFrom(tc.Value.Type) && ((SitecoreTypeConfiguration)tc.Value).TemplateId != (ID)null)
				: null;
            if (typeConfig == null || !typeConfig.Any())
                return _defaultDocumentMapper.GetPotentialCreatedTypes(baseType, executionContexts);

            return typeConfig.Select(tc => tc.Value.Type).ToList();
        }

        public object CreateElementInstance(Type baseType, IDictionary<string, object> fieldValues, IEnumerable<IExecutionContext> executionContexts)
        {
			var typeConfig = Context.Contexts.Any() ?
				Context.Contexts.Select(c => c.Value).Select(c => c.GetTypeConfigurationFromType<SitecoreTypeConfiguration>(baseType)).FirstOrDefault(c => c != null)
				: null;

			if (typeConfig == null || typeConfig.TemplateId == (ID)null || !fieldValues.ContainsKey(BuiltinFields.Template) || !fieldValues.ContainsKey(BuiltinFields.Group))
                return _defaultDocumentMapper.CreateElementInstance(baseType, fieldValues, executionContexts);

            var sitecoreService = new SitecoreContext();
            var typeCreationContext = new SitecoreTypeCreationContext
            {
                SitecoreService = sitecoreService,
                RequestedType = baseType,
                InferType = true,
                IsLazy = true,
                TemplateId = ID.Parse(fieldValues[BuiltinFields.Template]),
            };
            using (new SearchSwitcher())
            {
                var proxy = sitecoreService.InstantiateObject(typeCreationContext);
                SetupProxy(ID.Parse(fieldValues[BuiltinFields.Group]), fieldValues, proxy as IProxyTargetAccessor);
                return proxy;
            }
        }

        protected void SetupProxy(ID id, IDictionary<string, object> fieldValues, IProxyTargetAccessor target)
        {
            if (target == null) return;
            var searchInterceptor = target.GetInterceptors().FirstOrDefault(x => x is SearchInterceptor) as SearchInterceptor;
            if (searchInterceptor == null) return;

            searchInterceptor.Id = id;
            searchInterceptor.TypeConfiguration = new SitecoreContext().GlassContext.GetTypeConfiguration<SitecoreTypeConfiguration>(target);
        }


        public void Initialize(ISearchIndex searchIndex)
        {
            _defaultDocumentMapper.Initialize(searchIndex);
            _searchIndex = searchIndex;
        }
    }
}
