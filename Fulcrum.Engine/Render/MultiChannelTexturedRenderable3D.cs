// Copyright (C) 2025-2029 Convex89524
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3 (GPLv3 only).
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Veldrid;

namespace Fulcrum.Engine.Render;

public class MultiChannelTexturedRenderable3D : GeometryRenderable3D
{
    private readonly int _channelCount;
    private readonly TextureView[] _channelViews;
    private ResourceLayout _channelLayout;
    private ResourceSet _channelSet;

    public MultiChannelTexturedRenderable3D(
        string name, int channelCount,
        VertexLayoutDescription vertexLayout,
        GraphicsPipelineDescription pipelineDescription)
        : base(name, vertexLayout, pipelineDescription)
    {
        if (channelCount <= 0) throw new ArgumentOutOfRangeException(nameof(channelCount));
        _channelCount = channelCount;
        _channelViews = new TextureView[_channelCount];
    }

    public void SetChannelView(int index, TextureView view)
    {
        if ((uint)index >= (uint)_channelCount) throw new ArgumentOutOfRangeException(nameof(index));
        _channelViews[index] = view;
        if (_graphicsDevice != null && _channelLayout != null)
        {
            _channelSet?.Dispose();
            _channelSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
                new ResourceSetDescription(_channelLayout, BuildParams()));
        }
    }

    public void SetChannelViews(params TextureView[] views)
    {
        if (views == null || views.Length != _channelCount)
            throw new ArgumentException($"需要提供 {_channelCount} 个通道纹理。");
        for (int i = 0; i < _channelCount; i++) _channelViews[i] = views[i];
        if (_graphicsDevice != null && _channelLayout != null)
        {
            _channelSet?.Dispose();
            _channelSet = _graphicsDevice.ResourceFactory.CreateResourceSet(
                new ResourceSetDescription(_channelLayout, BuildParams()));
        }
    }

    public override void Initialize(GraphicsDevice gd, ResourceFactory factory)
    {
        base.Initialize(gd, factory);

        var elems = new List<ResourceLayoutElementDescription>(_channelCount * 2);
        for (int i = 0; i < _channelCount; i++)
        {
            elems.Add(new ResourceLayoutElementDescription($"Channel{i}", ResourceKind.TextureReadOnly, ShaderStages.Fragment));
            elems.Add(new ResourceLayoutElementDescription($"Channel{i}Sampler", ResourceKind.Sampler, ShaderStages.Fragment));
        }
        _channelLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(elems.ToArray()));

        var pd = PipelineDescription;
        var layouts = pd.ResourceLayouts?.ToList() ?? new List<ResourceLayout>();
        layouts.Add(_channelLayout);
        pd.ResourceLayouts = layouts.ToArray();
        PipelineDescription = pd;

        CreatePipeline(factory);

        _channelSet?.Dispose();
        _channelSet = factory.CreateResourceSet(new ResourceSetDescription(_channelLayout, BuildParams()));
    }

    private BindableResource[] BuildParams()
    {
        if (_graphicsDevice == null) return Array.Empty<BindableResource>();
        var list = new List<BindableResource>(_channelCount * 2);
        for (int i = 0; i < _channelCount; i++)
        {
            if (_channelViews[i] == null)
                throw new InvalidOperationException($"Channel {i} 纹理尚未设置。");
            list.Add(_channelViews[i]);
            list.Add(_graphicsDevice.PointSampler);
        }
        return list.ToArray();
    }

    public override void Draw(CommandList cl)
    {
        if (_vertexBuffer == null || _pipeline == null) return;

        cl.SetPipeline(_pipeline);
        cl.SetVertexBuffer(0, _vertexBuffer);
        if (_indexBuffer != null) cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);

        UpdateMatrices(cl);
        cl.SetGraphicsResourceSet(0, _resourceSet);
        cl.SetGraphicsResourceSet(1, _lightingSet);

        cl.SetGraphicsResourceSet((uint)(PipelineDescription.ResourceLayouts.Length - 1), _channelSet);

        if (_indexBuffer != null)
            cl.DrawIndexed((uint)(_indexBuffer.SizeInBytes / sizeof(ushort)), 1, 0, 0, 0);
        else
            cl.Draw((uint)(_vertexBuffer.SizeInBytes / VertexLayout.Elements.Sum(e => e.Format.GetSizeInBytes())));
    }
    
    public void SetMeshData(VertexPositionNormalTexture[] vertices, ushort[] indices)
    {
        SetVertexData(vertices);
        SetIndexData(indices);
    }

    public override void Dispose()
    {
        base.Dispose();
        _channelSet?.Dispose();
        _channelLayout?.Dispose();
    }
}
