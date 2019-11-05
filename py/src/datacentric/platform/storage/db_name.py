from datacentric.platform.storage.instance_type import InstanceType
from datacentric.types.record import TypedKey, RootRecord


class DbNameData(RootRecord['DbNameKey']):
    """This class enforces strict naming conventions
    for database naming. While format of the resulting database
    name is specific to data store type, it always consists
    of three tokens: instance_type, instance_name, and env_name.
    The meaning of InstanceName and EnvName tokens depends on
    the value of InstanceType enumeration.
    """

    __slots__ = ('instance_type', 'instance_name', 'env_name')

    instance_type: InstanceType
    """Some API functions are restricted based on the instance type."""

    instance_name: str
    """The meaning of instance name depends on the instance type.
    
     * For PROD, UAT, and DEV instance types, instance name
       identifies the endpoint.
    
     * For USER instance type, instance name is user alias.
    
     * For TEST instance type, instance name is the name of
       the unit test class.
    """
    env_name: str
    """The meaning of environment name depends on the instance type.
     * For PROD, UAT, DEV, and USER instance types, it is the
       name of the user environment selected in the client.
     * For TEST instance type, it is the test method name.
     """
    def __init__(self):
        super().__init__()
        self.instance_type = None
        self.instance_name = None
        self.env_name = None


class DbNameKey(TypedKey[DbNameData]):
    __slots__ = ('instance_type', 'instance_name', 'env_name')

    instance_type: InstanceType
    instance_name: str
    env_name: str

    def __init__(self):
        super().__init__()
        self.instance_type = None
        self.instance_name = None
        self.env_name = None
