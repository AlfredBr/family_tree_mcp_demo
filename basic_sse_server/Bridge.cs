using System.Threading.Channels;

namespace sse_server;

public class Bridge
{
    private readonly Channel<string> channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });

    public ChannelReader<string> Reader => channel.Reader;
    public ChannelWriter<string> Writer => channel.Writer;
}