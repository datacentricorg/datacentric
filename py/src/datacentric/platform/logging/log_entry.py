from datacentric.platform.logging.log_entry_type import LogEntryType


class LogEntry:
    """Log entry consists of formatted message and the original message parameter objects."""
    __max_message_param_length = 255

    def __init__(self, entry_type: LogEntryType, entry_sub_type: str, message: str, *message_params: object):
        formatted_message = LogEntry.format_message(message, *message_params)
        prefix = entry_type.name

        if entry_sub_type is not None and entry_sub_type != '':
            prefix = prefix + '.' + entry_sub_type

        self._entry_text = prefix + ': ' + formatted_message

    def __str__(self):
        return self._entry_text

    @staticmethod
    def format_message(message: str, *message_params: object) -> str:
        if len(message_params) > 0:
            formatted_message_params = []
            for arg in message_params:
                arg_str = str(arg)

                # Restrict message length if more than max length
                if len(arg_str) > LogEntry.__max_message_param_length:
                    substring_length: int = (LogEntry.__max_message_param_length - 5) // 2
                    head = arg_str[0:substring_length]
                    tail = arg_str[len(arg_str) - substring_length:len(arg_str)]
                    arg_str = head + ' ... ' + tail
                formatted_message_params.append(arg_str)

            result = message.format(formatted_message_params)

            # If the message ends with four dots (....) because the last argument is truncated
            # while its token is followed by a dot (.), reduce to three dots at the end (...)
            if result.endswith('....'):
                return result[0: len(result) - 1]

            return result

        # Do not perform substitution if no parameters are specified.
        # This will work even if the string contains {} characters
        else:
            return message
