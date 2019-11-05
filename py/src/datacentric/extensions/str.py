import re


def to_pascal_case(name: str) -> str:
    return ''.join(x for x in name.title() if not x == '_')


__first_cap_re = re.compile('(.)([A-Z][a-z]+)')
__all_cap_re = re.compile('([a-z0-9])([A-Z])')


def to_snake_case(name: str) -> str:
    s1 = __first_cap_re.sub(r'\1_\2', name)
    return __all_cap_re.sub(r'\1_\2', s1).lower()
