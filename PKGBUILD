# Maintainer: Mads Mogensen <mads256h at protonmail.com>

_pkgname=break-bot
pkgname=${_pkgname}-git
pkgver=dbd71bf
pkgrel=1
pkgdesc="A discord bot that tells you when to take a break"
arch=('x86_64')
url='https://github.com/mads256h/break-bot'
license=("GPL")
depends=("dotnet-runtime>=3.1.0")
makedepends=("dotnet-sdk>=3.1.0" "git")
source=("${_pkgname}::git+https://github.com/mads256h/break-bot.git")
sha256sums=('SKIP')

pkgver() {
  cd "$_pkgname"
  git describe --long --always | sed 's/\([^-]*-g\)/r\1/;s/-/./g'
}

prepare(){
  cd "${_pkgname}"
  mkdir -p ${srcdir}/nuget

  NUGET_PACKAGES=${srcdir}/nuget/packages \
  NUGET_HTTP_CACHE_PATH=${srcdir}/nuget/v3-cache \
  NUGET_PLUGINS_CACHE_PATH=${srcdir}/nuget/plugins-cache \
  dotnet restore \
  --runtime linux-x64 \
  --verbosity normal
}

build() {
  cd "${_pkgname}"
  export MSBUILDDISABLENODEREUSE=1

  dotnet build \
    --configuration Release \
    --no-restore
}

package() {
  install -Dm644 "${srcdir}/break-bot.service" "${pkgdir}/usr/lib/systemd/system/break-bot.service"
  cd ${_pkgname}
  #install -dm755 ${pkgdir}/usr/{bin,lib} "${pkgdir}/etc/${_pkgname}"
  dotnet publish \
  --runtime linux-x64 \
  --configuration Release \
  --self-contained false \
  --no-restore \
  -o "${pkgdir}/usr/lib/${_pkgname}"
  chmod +x "${pkgdir}/usr/lib/${_pkgname}/break-bot"
  mkdir "${pkgdir}/usr/bin"
  ln -s "/usr/lib/${_pkgname}/break-bot" "${pkgdir}/usr/bin/${_pkgname}"
}

