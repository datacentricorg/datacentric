import setuptools

with open("README.md", "r") as fh:
    long_description = fh.read()

setuptools.setup(
    name="datacentric",
    version="2.1.0",
    author="The DataCentric Authors",
    author_email="support@datacentric.org",
    description="Core services library for data-centric development.",
    long_description=long_description,
    long_description_content_type="text/markdown",
    install_requires=['typing_inspect>=0.4', 'numpy>=1.17', 'pymongo>=3.9.0'],
    url="https://github.com/datacentricorg/datacentric-py",
    packages=setuptools.find_packages(include=('datacentric', 'datacentric.*'), exclude=['tests', 'tests.*']),
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: Apache Software License",
        "Operating System :: OS Independent",
    ],
)
