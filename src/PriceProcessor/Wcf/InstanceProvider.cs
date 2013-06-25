using System;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using Castle.ActiveRecord;
using NHibernate;
using ISession = NHibernate.ISession;

namespace Inforoom.PriceProcessor.Wcf
{
	public class InstanceProvider : IInstanceProvider
	{
		private ISessionFactory factory;
		private Type type;
		private FieldInfo field;

		public InstanceProvider(Type type, FieldInfo field)
		{
			this.type = type;
			this.field = field;
			factory = ActiveRecordMediator
				.GetSessionFactoryHolder()
				.GetSessionFactory(typeof(ActiveRecordBase));
		}

		public object GetInstance(InstanceContext instanceContext)
		{
			var instance = Activator.CreateInstance(type);
			field.SetValue(instance, factory.OpenSession());
			return instance;
		}

		public object GetInstance(InstanceContext instanceContext, Message message)
		{
			return GetInstance(instanceContext);
		}

		public void ReleaseInstance(InstanceContext instanceContext, object instance)
		{
			var value = field.GetValue(instance) as ISession;
			if (value != null)
				value.Dispose();
		}
	}
}