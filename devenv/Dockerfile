FROM devenv-base:latest

# install additional dependencies
RUN sudo apk add  \
  rustup          \
  mingw-w64-gcc   \
  --no-cache

# set up rust for dev user
RUN /usr/bin/rustup-init -y
RUN echo '. "$HOME/.cargo/env"' >> $HOME/.profile

# set up windows target
RUN $HOME/.cargo/bin/rustup target add x86_64-pc-windows-gnu
