namespace NGIS.Message {
  public interface ISerializableMsg {
    int GetSerializedSize();
    int WriteTo(byte[] buffer, int offset);
  }
}