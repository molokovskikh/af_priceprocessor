using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Inforoom.PriceProcessor.Wcf
{
	public class RegisterSessionBehavior : IServiceBehavior
	{
		public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
		}

		public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
		{
		}

		public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
		{
			var endpoints = serviceHostBase.ChannelDispatchers.OfType<ChannelDispatcher>()
				.SelectMany(d => d.Endpoints)
				.Where(d => !d.IsSystemEndpoint);
			foreach (var endpoint in endpoints) {
				var type = endpoint.DispatchRuntime.Type;
				var field = type.GetField("Session", BindingFlags.Public | BindingFlags.Instance);
				if (field == null)
					return;

				endpoint.DispatchRuntime.InstanceProvider = new InstanceProvider(type, field);
			}
		}
	}
}