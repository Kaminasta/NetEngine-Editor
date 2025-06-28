namespace NetEngine
{
    public static class Shaders
    {
        public const string BaseVertexShader = @"
            #version 330 core
            layout(location = 0) in vec3 aPos;
            layout(location = 1) in vec2 aTexCoord; 

            uniform mat4 view;
            uniform mat4 projection;
            uniform mat4 model;

            out vec2 TexCoord; 

            void main()
            {
                gl_Position = projection * view * model * vec4(aPos, 1.0);
                TexCoord = aTexCoord; 
            }";

        public const string BaseFragmentShader = @"
            #version 330 core
            out vec4 FragColor;

            in vec2 TexCoord;

            float diamondPattern(vec2 uv, float scale)
            {
                uv *= scale;
                uv = abs(fract(uv) - 0.5);
                return 1.0 - step(uv.x + uv.y, 0.5);
            }

            void main()
            {
                float scale = 10.0;
                float pattern = diamondPattern(TexCoord, scale);
                vec3 color = mix(vec3(0.0, 0.0, 0.0), vec3(1.0, 0.0, 1.0), pattern);
                FragColor = vec4(color, 1.0);
            }";


        public const string GizmosVertexShader = @"
            #version 330 core
            layout (location = 0) in vec3 aPos;
            layout (location = 1) in vec3 aColor;

            out vec3 vColor;

            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;

            void main()
            {
                gl_Position = projection * view * model * vec4(aPos, 1.0);
                vColor = aColor;
            }";

        public const string GizmosFragmentShader = @"
            #version 330 core
            in vec3 vColor;
            out vec4 FragColor;

            void main()
            {
                FragColor = vec4(vColor, 1.0);
            }";

        public const string DefaultVertexShader = @"
            #version 330 core
            layout(location = 0) in vec3 aPos;
            layout(location = 1) in vec2 aTexCoord;
            layout(location = 2) in vec3 aNormal;     

            uniform mat4 model;
            uniform mat4 view;
            uniform mat4 projection;

            out vec2 TexCoord;
            out vec3 WorldPos;
            out vec3 Normal;

            void main()
            {
                vec4 worldPosition = model * vec4(aPos, 1.0);
                WorldPos = worldPosition.xyz;

                Normal = mat3(transpose(inverse(model))) * aNormal;

                TexCoord = aTexCoord;

                gl_Position = projection * view * worldPosition;
            }";

        public const string DefaultFragmentShader = @"
            #version 330 core
            out vec4 FragColor;

            in vec2 TexCoord;
            in vec3 WorldPos;
            in vec3 Normal;

            uniform samplerCube _skyBox;
            uniform vec3 _cameraPos; 
            uniform float _reflection;
            uniform vec4 _color;

            vec4 ApplyRefRefract(vec4 FinalColor, vec3 Normal)
            {
                vec3 CameraToPixel = normalize(WorldPos - _cameraPos);

                vec3 ReflectionDir = normalize(reflect(CameraToPixel, Normal));
                vec4 ColorReflect = vec4(texture(_skyBox, ReflectionDir).rgb, 1.0);

                vec3 RefractionDir = normalize(refract(CameraToPixel, Normal, 0.14));
                vec4 ColorRefract = vec4(texture(_skyBox, RefractionDir).rgb, 1.0);

                vec4 ColorRefractReflect = mix(ColorRefract, ColorReflect, _reflection);

                FinalColor = mix(FinalColor, ColorRefractReflect, _reflection);

                return FinalColor;
            }

            void main()
            {
                FragColor = ApplyRefRefract(_color, Normal) * _color;
            }";

        public const string SkyBoxVertexShader = @"
            #version 330 core

            layout(location = 0) in vec3 aPosition;

            out vec3 texCoords;

            uniform mat4 _projection;
            uniform mat4 _view;

            void main()
            {
                vec4 pos = _projection * _view * vec4(aPosition, 1.0f);
                gl_Position = vec4(pos.x, pos.y, pos.w, pos.w);
                texCoords = vec3(aPosition.x, aPosition.y, -aPosition.z);
            }";

        public const string SkyBoxFragmentShader = @"
            #version 330 core

            out vec4 FragColor;

            in vec3 texCoords;

            uniform samplerCube _skyBox;

            void main()
            {
                FragColor = texture(_skyBox, texCoords);
            }";

    }
}
