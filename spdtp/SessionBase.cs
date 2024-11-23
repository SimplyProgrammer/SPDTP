
public abstract class SessionBase<MetaT, MsgT, ResuT> where MsgT : SpdtpMessageBase
{
	protected MetaT metadata;

	protected Connection connection;

	public abstract ResuT handleIncomingMessage(MsgT message);

	public abstract void onKeepAlive();

	public SessionBase(Connection connection, MetaT metadata)
	{
		setMetadata(metadata);
		this.connection = connection;
	}

	public MetaT getMetadata()
	{
		return metadata;
	}

	public void setMetadata(MetaT newMetadata)
	{
		metadata = newMetadata;
	}
}