using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using TouchTogglerClassLibrary;

namespace TouchTogglerDeskBand
{
    class ServiceConnectionClient
    {
        public static ChannelFactory<IServiceConnection> factory = null;

        public static IServiceConnection Start(System.Action<object, EventArgs> onClose = null)
        {
            factory = new ChannelFactory<IServiceConnection>(
                new NetNamedPipeBinding(),
                new EndpointAddress("net.pipe://localhost/TouchTogglerService/Pipe")
            );
            if (onClose != null) factory.Closed += new EventHandler(onClose);
            return factory.CreateChannel();
        }

    }
}
