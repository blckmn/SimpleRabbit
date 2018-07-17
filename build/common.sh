
basepath="${PWD}"
artifacts="${basepath}/artifacts"
nuget_server="https://www.nuget.org/api/v2/package"
revision=${TRAVIS_BRANCH:="alpha"}
major="1"
minor="0"
buildnumber=${TRAVIS_BUILD_NUMBER:=1}
version="${major}.${minor}.${buildnumber}"

if [ "${revision}" != "release" ]; then
  version="${version}-${revision}"
fi
export VERSION=${version}

if [ -d ${artifacts} ]; then  
  rm -R ${artifacts}
fi

echo "Base path: ${basepath}"
echo "Artifacts: ${artifacts}"
echo "Revision:  ${revision}"
echo "Build #:   ${buildnumber}"
echo "Version:   ${version}"
